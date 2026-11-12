using Bookmarks.Api.Domain;

namespace Bookmarks.Api.DTOs;

/// <summary>Fields a caller supplies when creating or replacing a bookmark.</summary>
/// <param name="Title">Human-readable name for the link.</param>
/// <param name="Url">Absolute http or https address.</param>
/// <param name="Description">Optional note about why the link was saved.</param>
/// <param name="Kind">What sort of thing the link points at.</param>
/// <param name="Status">Where the bookmark sits in its reading lifecycle.</param>
/// <param name="IsArchived">Whether the bookmark is retired from the active list.</param>
public sealed record SaveBookmarkDto(
    string Title,
    string Url,
    string? Description,
    BookmarkKind Kind,
    BookmarkStatus Status,
    bool IsArchived);
