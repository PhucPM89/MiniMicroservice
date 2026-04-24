using AuthService.DTOs.Auth;
using AuthService.DTOs.Users;

namespace AuthService.Interfaces.Services;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
