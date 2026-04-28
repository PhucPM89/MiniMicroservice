namespace Shared.Messaging;

public sealed class FileImportRequestedMessage
{
    public Guid FileId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public Guid? CorrelationId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public string? FilePath { get; set; }
    public string FileContentBase64 { get; set; } = string.Empty;
}
