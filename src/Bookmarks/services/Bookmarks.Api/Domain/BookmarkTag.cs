namespace Bookmarks.Api.Domain;

/// <summary>Join row attaching a <see cref="Tag"/> to a <see cref="Bookmark"/>.</summary>
public sealed class BookmarkTag
{
    /// <summary>Identifier of the bookmark.</summary>
    public string BookmarkId { get; set; } = string.Empty;

    /// <summary>Identifier of the tag.</summary>
    public string TagId { get; set; } = string.Empty;

    /// <summary>When the tag was attached.</summary>
    public DateTime CreatedAtUtc { get; set; }
}
