namespace Shared.Messaging;

public sealed class FileImportResultMessage
{
    public Guid ImportBatchId { get; set; }
    public Guid FileId { get; set; }
    public Guid? CorrelationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
