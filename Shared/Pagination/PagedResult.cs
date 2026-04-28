namespace Shared.Pagination;

public sealed class PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();
    public int PageSize { get; init; }
    public bool HasMore { get; init; }
    public string? NextCursor { get; init; }
}
