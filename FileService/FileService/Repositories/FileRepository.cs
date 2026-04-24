using FileService.Entities;
using FileService.Infrastructure.Persistence;
using FileService.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FileService.Repositories;

public sealed class FileRepository(FileServiceDbContext dbContext) : IFileRepository
{
    public async Task AddAsync(FileRecord file, CancellationToken cancellationToken = default)
    {
        await dbContext.Files.AddAsync(file, cancellationToken);
    }

    public async Task<FileRecord?> GetByIdAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Files.FirstOrDefaultAsync(file => file.Id == fileId, cancellationToken);
    }
}
