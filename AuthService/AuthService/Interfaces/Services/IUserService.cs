using AuthService.DTOs.Users;
using Shared.Pagination;

namespace AuthService.Interfaces.Services;

public interface IUserService
{
    Task<PagedResult<UserResponse>> GetAsync(UserQueryParameters query, CancellationToken cancellationToken = default);
    Task<UserResponse> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateAsync(Guid actingUserId, Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid actingUserId, Guid userId, CancellationToken cancellationToken = default);
}
