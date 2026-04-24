using FileService.Models;
using Microsoft.AspNetCore.Http;
using Shared.Pagination;

namespace FileService.Interfaces.Services;

public interface IFileService
{
    Task<FileResponse> UploadAsync(IFormFile file, Guid uploadedByUserId, CancellationToken cancellationToken = default);
    Task<PagedResult<FileResponse>> GetAsync(
        FileQueryParameters query,
        Guid actingUserId,
        bool canViewAll,
        CancellationToken cancellationToken = default);
    Task<FileResponse> GetByIdAsync(Guid fileId, Guid actingUserId, bool canViewAll, CancellationToken cancellationToken = default);
}
