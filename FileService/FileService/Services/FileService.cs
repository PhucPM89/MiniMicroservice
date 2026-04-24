using FileService.Entities;
using FileService.Interfaces.Repositories;
using FileService.Interfaces.Services;
using FileService.Models;
using Models.Services;
using Shared.Constants;
using Shared.Exceptions;
using Shared.Messaging;
using Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace FileService.Services;

public sealed class FileService(
    IFileRepository fileRepository,
    IFileStorageService fileStorageService,
    Infrastructure.Persistence.FileServiceDbContext dbContext) : IFileService
{
    public async Task<FileResponse> UploadAsync(IFormFile file, Guid uploadedByUserId, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        var fileContentBase64 = await ReadContentAsBase64Async(file, cancellationToken);
        var storedFile = await fileStorageService.SaveAsync(file, cancellationToken);
        var uploadedAtUtc = DateTime.UtcNow;
        var correlationId = Guid.NewGuid();
        var fileRecord = new FileRecord
        {
            Id = Guid.NewGuid(),
            OriginalFileName = storedFile.OriginalFileName,
            StoredFileName = storedFile.StoredFileName,
            StoragePath = storedFile.RelativePath,
            ContentType = storedFile.ContentType,
            FileExtension = storedFile.FileExtension,
            SizeInBytes = storedFile.SizeInBytes,
            UploadedByUserId = uploadedByUserId,
            CorrelationId = correlationId,
            Status = FileStatuses.Queued,
            UploadedAtUtc = uploadedAtUtc,
            QueuedAtUtc = uploadedAtUtc
        };

        try
        {
            await fileRepository.AddAsync(fileRecord, cancellationToken);
            dbContext.OutboxMessages.Add(OutboxMessageFactory.Create(
                QueueConstants.FileImportExchange,
                QueueConstants.FileImportRoutingKey,
                fileRecord.Id.ToString("N"),
                new FileImportRequestedMessage
                {
                    FileId = fileRecord.Id,
                    UploadedByUserId = fileRecord.UploadedByUserId,
                    CorrelationId = fileRecord.CorrelationId,
                    OriginalFileName = fileRecord.OriginalFileName,
                    ContentType = fileRecord.ContentType,
                    SizeInBytes = fileRecord.SizeInBytes,
                    UploadedAtUtc = fileRecord.UploadedAtUtc,
                    FileContentBase64 = fileContentBase64
                }));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await fileStorageService.DeleteAsync(storedFile.RelativePath, cancellationToken);
            throw;
        }

        return MapToResponse(fileRecord);
    }

    public async Task<PagedResult<FileResponse>> GetAsync(
        FileQueryParameters query,
        Guid actingUserId,
        bool canViewAll,
        CancellationToken cancellationToken = default)
    {
        var pageSize = NormalizePageSize(query.PageSize);
        var cursor = CursorTokenSerializer.Decode(query.Cursor);

        var filesQuery = dbContext.Files.AsNoTracking().AsQueryable();

        if (!canViewAll)
        {
            filesQuery = filesQuery.Where(file => file.UploadedByUserId == actingUserId);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            filesQuery = filesQuery.Where(file => file.Status == query.Status.Trim());
        }

        if (canViewAll && query.UploadedByUserId.HasValue)
        {
            filesQuery = filesQuery.Where(file => file.UploadedByUserId == query.UploadedByUserId.Value);
        }

        if (query.CorrelationId.HasValue)
        {
            filesQuery = filesQuery.Where(file => file.CorrelationId == query.CorrelationId.Value);
        }

        if (cursor is not null)
        {
            var timestampUtc = cursor.TimestampUtc;
            var lastId = cursor.LastId;

            filesQuery = filesQuery.Where(file =>
                file.UploadedAtUtc < timestampUtc
                || (file.UploadedAtUtc == timestampUtc && file.Id.CompareTo(lastId) < 0));
        }

        var rows = await filesQuery
            .OrderByDescending(file => file.UploadedAtUtc)
            .ThenByDescending(file => file.Id)
            .Take(pageSize + 1)
            .Select(file => new FilePageRow(
                file.Id,
                file.UploadedAtUtc,
                new FileResponse
                {
                    Id = file.Id,
                    OriginalFileName = file.OriginalFileName,
                    StoredFileName = file.StoredFileName,
                    StoragePath = file.StoragePath,
                    ContentType = file.ContentType,
                    FileExtension = file.FileExtension,
                    SizeInBytes = file.SizeInBytes,
                    UploadedByUserId = file.UploadedByUserId,
                    CorrelationId = file.CorrelationId,
                    Status = file.Status,
                    ErrorMessage = file.ErrorMessage,
                    UploadedAtUtc = file.UploadedAtUtc
                }))
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > pageSize;
        var pageRows = hasMore ? rows.Take(pageSize).ToList() : rows;
        var items = pageRows.Select(row => row.Response).ToList();

        return new PagedResult<FileResponse>
        {
            Items = items,
            PageSize = pageSize,
            HasMore = hasMore,
            NextCursor = hasMore ? CreateNextCursor(pageRows) : null
        };
    }

    public async Task<FileResponse> GetByIdAsync(
        Guid fileId,
        Guid actingUserId,
        bool canViewAll,
        CancellationToken cancellationToken = default)
    {
        var fileRecord = await fileRepository.GetByIdAsync(fileId, cancellationToken)
            ?? throw new NotFoundException($"File '{fileId}' was not found.");

        if (!canViewAll && fileRecord.UploadedByUserId != actingUserId)
        {
            throw new ForbiddenException("You do not have permission to view this file.");
        }

        return MapToResponse(fileRecord);
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file is null)
        {
            throw new ValidationException(["A file is required."], "File upload failed.");
        }

        if (file.Length <= 0)
        {
            throw new ValidationException(["The uploaded file is empty."], "File upload failed.");
        }

        var fileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ValidationException(["The uploaded file name is invalid."], "File upload failed.");
        }

        var extension = Path.GetExtension(fileName);
        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(["Only CSV files are supported."], "File upload failed.");
        }
    }

    private static async Task<string> ReadContentAsBase64Async(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return Convert.ToBase64String(buffer.ToArray());
    }

    private static int NormalizePageSize(int pageSize)
    {
        return pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
    }

    private static string CreateNextCursor(IReadOnlyList<FilePageRow> items)
    {
        var lastItem = items[^1];

        return CursorTokenSerializer.Encode(new TimestampCursor
        {
            TimestampUtc = lastItem.UploadedAtUtc,
            LastId = lastItem.Id
        });
    }

    private static FileResponse MapToResponse(FileRecord fileRecord)
    {
        return new FileResponse
        {
            Id = fileRecord.Id,
            OriginalFileName = fileRecord.OriginalFileName,
            StoredFileName = fileRecord.StoredFileName,
            StoragePath = fileRecord.StoragePath,
            ContentType = fileRecord.ContentType,
            FileExtension = fileRecord.FileExtension,
            SizeInBytes = fileRecord.SizeInBytes,
            UploadedByUserId = fileRecord.UploadedByUserId,
            CorrelationId = fileRecord.CorrelationId,
            Status = fileRecord.Status,
            ErrorMessage = fileRecord.ErrorMessage,
            UploadedAtUtc = fileRecord.UploadedAtUtc
        };
    }

    private sealed record FilePageRow(Guid Id, DateTime UploadedAtUtc, FileResponse Response);
}
