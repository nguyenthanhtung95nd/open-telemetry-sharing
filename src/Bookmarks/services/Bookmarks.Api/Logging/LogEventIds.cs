namespace Bookmarks.Api.Logging;

/// <summary>
/// Event ids for this service. The 3000 range belongs to Bookmarks.Api, so an id never
/// collides with another service sharing the same log backend.
/// </summary>
public static class LogEventIds
{
    /// <summary>A bookmark collection was read.</summary>
    public const int BookmarksListed = 3001;

    /// <summary>A bookmark was created.</summary>
    public const int BookmarkCreated = 3002;

    /// <summary>A bookmark was updated.</summary>
    public const int BookmarkUpdated = 3003;

    /// <summary>A bookmark was deleted.</summary>
    public const int BookmarkDeleted = 3004;

    /// <summary>A caller addressed a bookmark that does not exist.</summary>
    public const int BookmarkNotFound = 3005;

    /// <summary>The set of tags on a bookmark was replaced.</summary>
    public const int BookmarkTagsUpserted = 3006;

    /// <summary>A tag was created.</summary>
    public const int TagCreated = 3101;

    /// <summary>A tag was deleted.</summary>
    public const int TagDeleted = 3102;

    /// <summary>A tag name was already taken.</summary>
    public const int TagNameConflict = 3103;
}
