namespace FileService.Entities;

public sealed class FileRecord
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
    public Guid? CorrelationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public DateTime? QueuedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
