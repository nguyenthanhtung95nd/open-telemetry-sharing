using Bookmarks.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Bookmarks.Api.DTOs;

/// <summary>Filtering, sorting and paging options for the bookmark collection.</summary>
public sealed record BookmarksQueryParameters
{
    /// <summary>Default page size when the caller does not ask for one.</summary>
    public const int DefaultPageSize = 10;

    /// <summary>
    /// Largest page a caller may request.
    /// </summary>
    /// <remarks>
    /// Without a ceiling a single request can read the whole table, which is a denial of
    /// service with extra steps and shows up as one enormous database span.
    /// </remarks>
    public const int MaxPageSize = 100;

    /// <summary>Free-text match against title and description.</summary>
    [FromQuery(Name = "q")]
    public string? Search { get; init; }

    /// <summary>Restrict to one kind of bookmark.</summary>
    public BookmarkKind? Kind { get; init; }

    /// <summary>Restrict to one reading status.</summary>
    public BookmarkStatus? Status { get; init; }

    /// <summary>Sort expression, for example <c>title</c> or <c>title desc</c>.</summary>
    public string? Sort { get; init; }

    /// <summary>One-based page index.</summary>
    public int Page { get; init; } = 1;

    /// <summary>How many items to return, capped at <see cref="MaxPageSize"/>.</summary>
    public int PageSize { get; init; } = DefaultPageSize;
}
