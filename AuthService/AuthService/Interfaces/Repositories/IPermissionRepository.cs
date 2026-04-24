using AuthService.Entities;

namespace AuthService.Interfaces.Repositories;

public interface IPermissionRepository
{
    Task<IReadOnlyCollection<Permission>> GetByCodesAsync(IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string permissionCode, CancellationToken cancellationToken = default);
}
