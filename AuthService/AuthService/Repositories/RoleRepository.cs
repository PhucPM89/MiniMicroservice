using AuthService.Entities;
using AuthService.Infrastructure.Persistence;
using AuthService.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

public sealed class RoleRepository(AuthDbContext dbContext) : IRoleRepository
{
    public async Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .Include(role => role.RolePermissions)
                .ThenInclude(rolePermission => rolePermission.Permission)
            .FirstOrDefaultAsync(role => role.Name == roleName, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Role>> GetByNamesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken = default)
    {
        var roleNameSet = roleNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return await dbContext.Roles
            .Where(role => roleNameSet.Contains(role.Name))
            .Include(role => role.RolePermissions)
                .ThenInclude(rolePermission => rolePermission.Permission)
            .ToListAsync(cancellationToken);
    }
}
