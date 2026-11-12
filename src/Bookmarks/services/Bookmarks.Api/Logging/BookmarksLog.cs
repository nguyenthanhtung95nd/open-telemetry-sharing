namespace Bookmarks.Api.Logging;

/// <summary>
/// Every log message Bookmarks.Api emits.
/// </summary>
/// <remarks>
/// No message here may carry personally identifiable information, a request body, a saved
/// URL or a credential. Log records are shipped to a shared backend and retained far longer
/// than the request that produced them. Identifiers and counts only.
/// </remarks>
public static partial class BookmarksLog
{
    /// <summary>Logs that a bookmark collection was read.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="bookmarkCount">How many bookmarks were returned.</param>
    [LoggerMessage(
        EventId = LogEventIds.BookmarksListed,
        Level = LogLevel.Information,
        Message = "Listed {BookmarkCount} bookmarks")]
    public static partial void BookmarksListed(this ILogger logger, int bookmarkCount);

    /// <summary>Logs that a bookmark was created.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="bookmarkId">Identifier of the new bookmark.</param>
    [LoggerMessage(
        EventId = LogEventIds.BookmarkCreated,
        Level = LogLevel.Information,
        Message = "Bookmark {BookmarkId} created")]
    public static partial void BookmarkCreated(this ILogger logger, string bookmarkId);

    /// <summary>Logs that a bookmark was updated.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="bookmarkId">Identifier of the bookmark.</param>
    [LoggerMessage(
        EventId = LogEventIds.BookmarkUpdated,
        Level = LogLevel.Information,
        Message = "Bookmark {BookmarkId} updated")]
    public static partial void BookmarkUpdated(this ILogger logger, string bookmarkId);

    /// <summary>Logs that a bookmark was deleted.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="bookmarkId">Identifier of the bookmark.</param>
    [LoggerMessage(
        EventId = LogEventIds.BookmarkDeleted,
        Level = LogLevel.Information,
        Message = "Bookmark {BookmarkId} deleted")]
    public static partial void BookmarkDeleted(this ILogger logger, string bookmarkId);

    /// <summary>Logs that a caller addressed a bookmark that does not exist.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="bookmarkId">Identifier that was asked for.</param>
    [LoggerMessage(
        EventId = LogEventIds.BookmarkNotFound,
        Level = LogLevel.Warning,
        Message = "Bookmark {BookmarkId} was not found")]
    public static partial void BookmarkNotFound(this ILogger logger, string bookmarkId);

    /// <summary>Logs that the set of tags on a bookmark was replaced.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="bookmarkId">Identifier of the bookmark.</param>
    /// <param name="tagCount">How many tags the bookmark now carries.</param>
    [LoggerMessage(
        EventId = LogEventIds.BookmarkTagsUpserted,
        Level = LogLevel.Information,
        Message = "Bookmark {BookmarkId} now carries {TagCount} tags")]
    public static partial void BookmarkTagsUpserted(
        this ILogger logger, string bookmarkId, int tagCount);

    /// <summary>Logs that a tag was created.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="tagId">Identifier of the new tag.</param>
    [LoggerMessage(
        EventId = LogEventIds.TagCreated,
        Level = LogLevel.Information,
        Message = "Tag {TagId} created")]
    public static partial void TagCreated(this ILogger logger, string tagId);

    /// <summary>Logs that a tag was deleted.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="tagId">Identifier of the tag.</param>
    [LoggerMessage(
        EventId = LogEventIds.TagDeleted,
        Level = LogLevel.Information,
        Message = "Tag {TagId} deleted")]
    public static partial void TagDeleted(this ILogger logger, string tagId);

    /// <summary>Logs that a tag name was already taken.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="tagName">The name that collided.</param>
    [LoggerMessage(
        EventId = LogEventIds.TagNameConflict,
        Level = LogLevel.Warning,
        Message = "Tag name {TagName} is already taken")]
    public static partial void TagNameConflict(this ILogger logger, string tagName);
}
