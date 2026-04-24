using AuthService.DTOs.Users;
using AuthService.Entities;

namespace AuthService.Common;

public static class UserAccessResolver
{
    public static IReadOnlyCollection<string> GetRoleNames(User user)
    {
        return user.UserRoles
            .Select(userRole => userRole.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(roleName => roleName)
            .ToArray();
    }

    public static IReadOnlyCollection<string> GetEffectivePermissions(User user)
    {
        var deniedPermissions = user.UserPermissions
            .Where(userPermission => !userPermission.IsGranted)
            .Select(userPermission => userPermission.Permission.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var effectivePermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var permissionCode in user.UserRoles
                     .SelectMany(userRole => userRole.Role.RolePermissions)
                     .Select(rolePermission => rolePermission.Permission.Code))
        {
            if (!deniedPermissions.Contains(permissionCode))
            {
                effectivePermissions.Add(permissionCode);
            }
        }

        foreach (var permissionCode in user.UserPermissions
                     .Where(userPermission => userPermission.IsGranted)
                     .Select(userPermission => userPermission.Permission.Code))
        {
            effectivePermissions.Add(permissionCode);
        }

        return effectivePermissions
            .OrderBy(permissionCode => permissionCode)
            .ToArray();
    }

    public static IReadOnlyCollection<string> GetDirectlyGrantedPermissions(User user)
    {
        return user.UserPermissions
            .Where(userPermission => userPermission.IsGranted)
            .Select(userPermission => userPermission.Permission.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permissionCode => permissionCode)
            .ToArray();
    }

    public static IReadOnlyCollection<string> GetDirectlyDeniedPermissions(User user)
    {
        return user.UserPermissions
            .Where(userPermission => !userPermission.IsGranted)
            .Select(userPermission => userPermission.Permission.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permissionCode => permissionCode)
            .ToArray();
    }

    public static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            IsActive = user.IsActive,
            Roles = GetRoleNames(user),
            EffectivePermissions = GetEffectivePermissions(user),
            DirectGrantedPermissions = GetDirectlyGrantedPermissions(user),
            DirectDeniedPermissions = GetDirectlyDeniedPermissions(user),
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        };
    }
}
