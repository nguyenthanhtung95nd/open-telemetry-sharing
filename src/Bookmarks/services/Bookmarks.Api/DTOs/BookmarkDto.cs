using Bookmarks.Api.Domain;

namespace Bookmarks.Api.DTOs;

/// <summary>A bookmark as returned over the wire.</summary>
/// <param name="Id">Identifier of the bookmark.</param>
/// <param name="Title">Human-readable name for the link.</param>
/// <param name="Url">Absolute address the bookmark points at.</param>
/// <param name="Description">Optional note about why the link was saved.</param>
/// <param name="Kind">What sort of thing the link points at.</param>
/// <param name="Status">Where the bookmark sits in its reading lifecycle.</param>
/// <param name="IsArchived">Whether the bookmark has been retired from the active list.</param>
/// <param name="Tags">Names of the tags attached to this bookmark.</param>
/// <param name="CreatedAtUtc">When the bookmark was first saved.</param>
/// <param name="UpdatedAtUtc">When the bookmark was last edited, if ever.</param>
/// <param name="LastVisitedAtUtc">When the link was last opened, if ever.</param>
public sealed record BookmarkDto(
    string Id,
    string Title,
    string Url,
    string? Description,
    BookmarkKind Kind,
    BookmarkStatus Status,
    bool IsArchived,
    IReadOnlyList<string> Tags,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? LastVisitedAtUtc);
