using AuthService.Entities;
using Shared.Pagination;

namespace AuthService.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdWithAccessAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<User>> GetPageWithAccessAsync(string? roleName, TimestampCursor? cursor, int take, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
}
