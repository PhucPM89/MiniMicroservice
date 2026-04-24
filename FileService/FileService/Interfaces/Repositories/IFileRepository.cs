using FileService.Entities;

namespace FileService.Interfaces.Repositories;

public interface IFileRepository
{
    Task AddAsync(FileRecord file, CancellationToken cancellationToken = default);
    Task<FileRecord?> GetByIdAsync(Guid fileId, CancellationToken cancellationToken = default);
}
