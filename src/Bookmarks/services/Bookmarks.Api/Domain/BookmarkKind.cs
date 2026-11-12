namespace Bookmarks.Api.Domain;

/// <summary>What sort of thing a bookmark points at.</summary>
public enum BookmarkKind
{
    /// <summary>Unset. Never valid on a persisted bookmark.</summary>
    None = 0,

    /// <summary>Prose meant to be read once.</summary>
    Article = 1,

    /// <summary>Recorded video.</summary>
    Video = 2,

    /// <summary>Material returned to repeatedly, such as documentation.</summary>
    Reference = 3
}
