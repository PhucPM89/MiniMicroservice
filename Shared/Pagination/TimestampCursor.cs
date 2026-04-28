namespace Shared.Pagination;

public sealed class TimestampCursor
{
    public DateTime TimestampUtc { get; init; }
    public Guid LastId { get; init; }
}
