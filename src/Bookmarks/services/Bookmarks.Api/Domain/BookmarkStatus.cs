namespace Bookmarks.Api.Domain;

/// <summary>Where a bookmark sits in its reading lifecycle.</summary>
public enum BookmarkStatus
{
    /// <summary>Unset. Never valid on a persisted bookmark.</summary>
    None = 0,

    /// <summary>Saved but not yet opened.</summary>
    Unread = 1,

    /// <summary>Opened and finished.</summary>
    Read = 2
}
