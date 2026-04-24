using FileService.Configuration;
using FileService.Interfaces.Services;
using FileService.Models;
using Microsoft.Extensions.Options;

namespace FileService.Services;

public sealed class LocalFileStorageService(
    IOptions<FileStorageOptions> options,
    IWebHostEnvironment environment) : IFileStorageService
{
    public async Task<StoredFileResult> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var originalFileName = Path.GetFileName(file.FileName);
        var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{fileExtension}";
        var relativeDirectory = Path.Combine(DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"), DateTime.UtcNow.ToString("dd"));
        var relativePath = Path.Combine(relativeDirectory, storedFileName);
        var absoluteDirectory = ResolveStorageRootPath();
        var absolutePath = Path.Combine(absoluteDirectory, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var stream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "text/csv" : file.ContentType;
        return new StoredFileResult(
            OriginalFileName: originalFileName,
            StoredFileName: storedFileName,
            RelativePath: relativePath.Replace('\\', '/'),
            ContentType: contentType,
            FileExtension: fileExtension,
            SizeInBytes: file.Length);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.Combine(ResolveStorageRootPath(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    private string ResolveStorageRootPath()
    {
        return Path.IsPathRooted(options.Value.RootPath)
            ? options.Value.RootPath
            : Path.Combine(environment.ContentRootPath, options.Value.RootPath);
    }
}
