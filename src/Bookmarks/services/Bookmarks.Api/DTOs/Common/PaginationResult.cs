namespace Bookmarks.Api.DTOs.Common;

/// <summary>One page of a collection, with enough context to request the next.</summary>
/// <typeparam name="T">Item type.</typeparam>
/// <param name="Items">The items on this page. Empty rather than null when none match.</param>
/// <param name="Page">One-based index of this page.</param>
/// <param name="PageSize">How many items a full page holds.</param>
/// <param name="TotalCount">How many items match the query across all pages.</param>
public sealed record PaginationResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    /// <summary>How many pages the matching items span.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Whether a page exists before this one.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Whether a page exists after this one.</summary>
    public bool HasNextPage => Page < TotalPages;
}
