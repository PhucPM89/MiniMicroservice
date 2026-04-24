namespace TransactionService.Models;

public sealed class TransactionResponse
{
    public Guid Id { get; set; }
    public Guid ImportBatchId { get; set; }
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? RawLineNumber { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
