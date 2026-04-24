namespace TransactionService.Models;

public sealed class TransactionQueryParameters
{
    public Guid? FileId { get; set; }
    public Guid? ImportBatchId { get; set; }
    public string? TransactionId { get; set; }
    public string? Type { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? Cursor { get; set; }
    public int PageSize { get; set; } = 20;
}
