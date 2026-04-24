using AuthService.Entities;

namespace AuthService.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Role>> GetByNamesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken = default);
}
