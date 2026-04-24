using AuthService.Infrastructure.Persistence;
using AuthService.Interfaces.Repositories;

namespace AuthService.Repositories;

public sealed class UnitOfWork(AuthDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
