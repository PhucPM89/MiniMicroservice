namespace TransactionService.Entities;

public sealed class TransactionErrorRecord
{
    public Guid Id { get; set; }
    public Guid ImportBatchId { get; set; }
    public int LineNumber { get; set; }
    public string? RawRecord { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public ImportBatch ImportBatch { get; set; } = null!;
}
