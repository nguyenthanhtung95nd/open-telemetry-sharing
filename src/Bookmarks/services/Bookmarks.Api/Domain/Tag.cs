namespace Bookmarks.Api.Domain;

/// <summary>A label that can be attached to any number of bookmarks.</summary>
public sealed class Tag
{
    /// <summary>Identifier, a type-prefixed UUIDv7.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name. Unique, compared without regard to case.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional note about what the tag is for.</summary>
    public string? Description { get; set; }

    /// <summary>When the tag was created.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>When the tag was last edited, if ever.</summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Bookmarks carrying this tag, through the join table.</summary>
    public List<Bookmark> Bookmarks { get; } = [];

    /// <summary>Creates an identifier for a new tag.</summary>
    /// <returns>A type-prefixed, time-ordered identifier.</returns>
    public static string CreateId() => $"t_{Guid.CreateVersion7()}";
}
