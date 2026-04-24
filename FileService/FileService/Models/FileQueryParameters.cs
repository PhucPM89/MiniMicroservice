namespace FileService.Models;

public sealed class FileQueryParameters
{
    public string? Status { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public Guid? CorrelationId { get; set; }
    public string? Cursor { get; set; }
    public int PageSize { get; set; } = 20;
}
