using AuthService.Common;
using AuthService.DTOs.Auth;
using AuthService.DTOs.Users;
using AuthService.Entities;
using AuthService.Interfaces.Repositories;
using AuthService.Interfaces.Services;
using Shared.Constants;
using Shared.Exceptions;

namespace AuthService.Services;

public sealed class AuthenticationService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        await EnsureEmailAvailableAsync(normalizedEmail, cancellationToken);

        var userRole = await roleRepository.GetByNameAsync(RoleConstants.User, cancellationToken)
            ?? throw new NotFoundException($"Default role '{RoleConstants.User}' was not found.");

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            IsActive = true
        };

        user.UserRoles.Add(new UserRole
        {
            User = user,
            Role = userRole,
            RoleId = userRole.Id
        });

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UserAccessResolver.ToResponse(user);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await userRepository.GetByEmailWithAccessAsync(normalizedEmail, cancellationToken);
        if (user is null || !passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("This account has been deactivated.");
        }

        var roles = UserAccessResolver.GetRoleNames(user);
        var permissions = UserAccessResolver.GetEffectivePermissions(user);
        var token = tokenService.CreateAccessToken(user, roles, permissions);

        return new LoginResponse
        {
            AccessToken = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = UserAccessResolver.ToResponse(user)
        };
    }

    private async Task EnsureEmailAvailableAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        if (await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            throw new ValidationException(["Email is already in use."], "Registration failed.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
