using AuthService.Entities;
using AuthService.Infrastructure.Persistence;
using AuthService.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Pagination;

namespace AuthService.Repositories;

public sealed class UserRepository(AuthDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdWithAccessAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await CreateAccessQuery().FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default)
    {
        return await CreateAccessQuery().FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyCollection<User>> GetPageWithAccessAsync(
        string? roleName,
        TimestampCursor? cursor,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = CreateAccessQuery()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            query = query.Where(user => user.UserRoles.Any(userRole => userRole.Role.Name == roleName));
        }

        if (cursor is not null)
        {
            var timestampUtc = cursor.TimestampUtc;
            var lastId = cursor.LastId;

            query = query.Where(user =>
                user.CreatedAtUtc < timestampUtc
                || (user.CreatedAtUtc == timestampUtc && user.Id.CompareTo(lastId) < 0));
        }

        return await query
            .OrderByDescending(user => user.CreatedAtUtc)
            .ThenByDescending(user => user.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        dbContext.Users.Update(user);
    }

    private IQueryable<User> CreateAccessQuery()
    {
        return dbContext.Users
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                    .ThenInclude(role => role.RolePermissions)
                        .ThenInclude(rolePermission => rolePermission.Permission)
            .Include(user => user.UserPermissions)
                .ThenInclude(userPermission => userPermission.Permission);
    }
}
