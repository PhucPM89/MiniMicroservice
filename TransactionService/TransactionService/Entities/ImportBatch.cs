namespace TransactionService.Entities;

public sealed class ImportBatch
{
    public Guid Id { get; set; }
    public Guid FileId { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<TransactionRecord> Transactions { get; set; } = new List<TransactionRecord>();
    public ICollection<TransactionErrorRecord> Errors { get; set; } = new List<TransactionErrorRecord>();
}
