using AuthService.Common;
using AuthService.DTOs.Users;
using AuthService.Entities;
using AuthService.Interfaces.Repositories;
using AuthService.Interfaces.Services;
using Shared.Constants;
using Shared.Exceptions;
using Shared.Pagination;

namespace AuthService.Services;

public sealed class UserService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher) : IUserService
{
    public async Task<PagedResult<UserResponse>> GetAsync(UserQueryParameters query, CancellationToken cancellationToken = default)
    {
        var pageSize = NormalizePageSize(query.PageSize);
        var cursor = CursorTokenSerializer.Decode(query.Cursor);
        var roleName = NormalizeOptionalRole(query.Role);
        var users = await userRepository.GetPageWithAccessAsync(roleName, cursor, pageSize + 1, cancellationToken);

        var hasMore = users.Count > pageSize;
        var pageUsers = hasMore ? users.Take(pageSize).ToList() : users.ToList();
        var items = pageUsers
            .Select(UserAccessResolver.ToResponse)
            .ToList();

        return new PagedResult<UserResponse>
        {
            Items = items,
            PageSize = pageSize,
            HasMore = hasMore,
            NextCursor = hasMore ? CreateNextCursor(pageUsers) : null
        };
    }

    public async Task<UserResponse> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        return UserAccessResolver.ToResponse(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        await EnsureEmailAvailableAsync(normalizedEmail, null, cancellationToken);

        ValidatePermissionAssignments(request.GrantedPermissionCodes, request.DeniedPermissionCodes);

        var resolvedRoles = await ResolveRolesAsync(
            request.RoleNames.Count == 0 ? [RoleConstants.User] : request.RoleNames,
            cancellationToken);

        var resolvedPermissions = await ResolveDirectPermissionsAsync(
            request.GrantedPermissionCodes,
            request.DeniedPermissionCodes,
            cancellationToken);

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            IsActive = request.IsActive
        };

        ApplyRoles(user, resolvedRoles);
        ApplyDirectPermissions(user, resolvedPermissions);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UserAccessResolver.ToResponse(user);
    }

    public async Task<UserResponse> UpdateAsync(Guid actingUserId, Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        var isSelfUpdate = actingUserId == userId;

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalizedEmail = NormalizeEmail(request.Email);
            await EnsureEmailAvailableAsync(normalizedEmail, user.Id, cancellationToken);
            user.Email = normalizedEmail;
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = passwordHasher.HashPassword(request.Password);
        }

        if (request.IsActive.HasValue)
        {
            if (isSelfUpdate && !request.IsActive.Value)
            {
                throw new ValidationException(["You cannot deactivate your own account."], "User update failed.");
            }

            user.IsActive = request.IsActive.Value;
        }

        if (request.RoleNames is not null)
        {
            if (isSelfUpdate && !request.RoleNames.Contains(RoleConstants.Admin, StringComparer.OrdinalIgnoreCase))
            {
                throw new ValidationException(["You cannot remove your own Admin role."], "User update failed.");
            }

            var resolvedRoles = await ResolveRolesAsync(
                request.RoleNames.Count == 0 ? [RoleConstants.User] : request.RoleNames,
                cancellationToken);

            ApplyRoles(user, resolvedRoles);
        }

        if (request.GrantedPermissionCodes is not null || request.DeniedPermissionCodes is not null)
        {
            var granted = request.GrantedPermissionCodes ?? [];
            var denied = request.DeniedPermissionCodes ?? [];

            ValidatePermissionAssignments(granted, denied);

            var resolvedPermissions = await ResolveDirectPermissionsAsync(granted, denied, cancellationToken);
            ApplyDirectPermissions(user, resolvedPermissions);
        }

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UserAccessResolver.ToResponse(user);
    }

    public async Task DeleteAsync(Guid actingUserId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (actingUserId == userId)
        {
            throw new ValidationException(["You cannot deactivate your own account."], "User delete failed.");
        }

        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        user.IsActive = false;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetUserOrThrowAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await userRepository.GetByIdWithAccessAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");
    }

    private async Task EnsureEmailAvailableAsync(string normalizedEmail, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var existingUser = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is not null && existingUser.Id != currentUserId)
        {
            throw new ValidationException(["Email is already in use."], "User validation failed.");
        }
    }

    private async Task<IReadOnlyCollection<Role>> ResolveRolesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken)
    {
        var normalizedRoleNames = roleNames
            .Select(NormalizeRequiredText)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var roles = await roleRepository.GetByNamesAsync(normalizedRoleNames, cancellationToken);
        var missingRoleNames = normalizedRoleNames
            .Except(roles.Select(role => role.Name), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (missingRoleNames.Length > 0)
        {
            throw new ValidationException(
                missingRoleNames.Select(roleName => $"Role '{roleName}' was not found."),
                "Role validation failed.");
        }

        return roles;
    }

    private async Task<IReadOnlyCollection<ResolvedUserPermission>> ResolveDirectPermissionsAsync(
        IEnumerable<string> grantedPermissionCodes,
        IEnumerable<string> deniedPermissionCodes,
        CancellationToken cancellationToken)
    {
        var grantedCodes = grantedPermissionCodes
            .Select(NormalizeRequiredText)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var deniedCodes = deniedPermissionCodes
            .Select(NormalizeRequiredText)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var requestedCodes = grantedCodes
            .Concat(deniedCodes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requestedCodes.Length == 0)
        {
            return Array.Empty<ResolvedUserPermission>();
        }

        var permissions = await permissionRepository.GetByCodesAsync(requestedCodes, cancellationToken);
        var missingCodes = requestedCodes
            .Except(permissions.Select(permission => permission.Code), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (missingCodes.Length > 0)
        {
            throw new ValidationException(
                missingCodes.Select(permissionCode => $"Permission '{permissionCode}' was not found."),
                "Permission validation failed.");
        }

        return permissions
            .Select(permission => new ResolvedUserPermission(permission, grantedCodes.Contains(permission.Code, StringComparer.OrdinalIgnoreCase)))
            .ToArray();
    }

    private static void ApplyRoles(User user, IEnumerable<Role> roles)
    {
        user.UserRoles.Clear();

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole
            {
                User = user,
                Role = role,
                RoleId = role.Id
            });
        }
    }

    private static void ApplyDirectPermissions(User user, IEnumerable<ResolvedUserPermission> permissions)
    {
        user.UserPermissions.Clear();

        foreach (var resolvedPermission in permissions)
        {
            user.UserPermissions.Add(new UserPermission
            {
                User = user,
                Permission = resolvedPermission.Permission,
                PermissionId = resolvedPermission.Permission.Id,
                IsGranted = resolvedPermission.IsGranted
            });
        }
    }

    private static void ValidatePermissionAssignments(IEnumerable<string> grantedPermissionCodes, IEnumerable<string> deniedPermissionCodes)
    {
        var grantedCodes = grantedPermissionCodes
            .Select(NormalizeRequiredText)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var deniedCodes = deniedPermissionCodes
            .Select(NormalizeRequiredText)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var duplicates = grantedCodes.Intersect(deniedCodes, StringComparer.OrdinalIgnoreCase).ToArray();
        if (duplicates.Length > 0)
        {
            throw new ValidationException(
                duplicates.Select(permissionCode => $"Permission '{permissionCode}' cannot be granted and denied at the same time."),
                "Permission validation failed.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeRequiredText(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptionalRole(string? role)
    {
        return string.IsNullOrWhiteSpace(role) ? null : role.Trim();
    }

    private static int NormalizePageSize(int pageSize)
    {
        return pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
    }

    private static string CreateNextCursor(IReadOnlyList<User> users)
    {
        var lastUser = users[^1];

        return CursorTokenSerializer.Encode(new TimestampCursor
        {
            TimestampUtc = lastUser.CreatedAtUtc,
            LastId = lastUser.Id
        });
    }

    private sealed record ResolvedUserPermission(Permission Permission, bool IsGranted);
}
