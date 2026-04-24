using AuthService.Entities;
using AuthService.Infrastructure.Persistence;
using AuthService.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

public sealed class PermissionRepository(AuthDbContext dbContext) : IPermissionRepository
{
    public async Task<IReadOnlyCollection<Permission>> GetByCodesAsync(IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var permissionCodeSet = permissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return await dbContext.Permissions
            .Where(permission => permissionCodeSet.Contains(permission.Code))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        return await dbContext.Permissions
            .AnyAsync(permission => permission.Code == permissionCode, cancellationToken);
    }
}
