namespace Bookmarks.Api.Domain;

/// <summary>A saved link.</summary>
public sealed class Bookmark
{
    /// <summary>Identifier, a type-prefixed UUIDv7 so ordering by id matches creation order.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable name for the link.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Absolute http or https address the bookmark points at.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Optional note about why the link was saved.</summary>
    public string? Description { get; set; }

    /// <summary>What sort of thing the link points at.</summary>
    public BookmarkKind Kind { get; set; }

    /// <summary>Where the bookmark sits in its reading lifecycle.</summary>
    public BookmarkStatus Status { get; set; }

    /// <summary>Whether the bookmark has been retired from the active list.</summary>
    public bool IsArchived { get; set; }

    /// <summary>When the bookmark was first saved.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>When the bookmark was last edited, if ever.</summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>When the link was last opened, if ever.</summary>
    public DateTime? LastVisitedAtUtc { get; set; }

    /// <summary>Tags attached to this bookmark, through the join table.</summary>
    public List<Tag> Tags { get; } = [];

    /// <summary>Creates an identifier for a new bookmark.</summary>
    /// <returns>A type-prefixed, time-ordered identifier.</returns>
    public static string CreateId() => $"b_{Guid.CreateVersion7()}";
}
