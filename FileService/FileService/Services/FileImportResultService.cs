using Models.Services;
using Shared.Constants;
using Shared.Exceptions;
using Shared.Messaging;

namespace FileService.Services;

public sealed class FileImportResultService(Infrastructure.Persistence.FileServiceDbContext dbContext)
{
    public async Task ApplyAsync(FileImportResultMessage message, CancellationToken cancellationToken = default)
    {
        var fileRecord = await dbContext.Files.FindAsync([message.FileId], cancellationToken);
        if (fileRecord is null)
        {
            throw new NotFoundException($"File '{message.FileId}' was not found.");
        }

        fileRecord.Status = message.Status switch
        {
            FileImportResultStatuses.Completed => FileStatuses.Completed,
            FileImportResultStatuses.Failed => FileStatuses.Failed,
            _ => throw new ValidationException([$"Unsupported file import result status '{message.Status}'."], "File status update failed.")
        };

        fileRecord.ErrorMessage = string.IsNullOrWhiteSpace(message.ErrorMessage)
            ? null
            : message.ErrorMessage;
        fileRecord.ProcessedAtUtc = message.CompletedAtUtc ?? DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
