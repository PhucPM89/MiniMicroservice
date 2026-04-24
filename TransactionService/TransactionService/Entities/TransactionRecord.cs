namespace TransactionService.Entities;

public sealed class TransactionRecord
{
    public Guid Id { get; set; }
    public Guid ImportBatchId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? RawLineNumber { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ImportBatch ImportBatch { get; set; } = null!;
}
