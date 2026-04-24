using FileService.Models;
using Microsoft.AspNetCore.Http;

namespace FileService.Interfaces.Services;

public interface IFileStorageService
{
    Task<StoredFileResult> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}
