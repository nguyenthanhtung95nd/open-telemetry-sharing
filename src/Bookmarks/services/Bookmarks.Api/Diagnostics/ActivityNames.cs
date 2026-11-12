namespace Bookmarks.Api.Diagnostics;

/// <summary>
/// Names of the spans this service starts by hand.
/// </summary>
/// <remarks>
/// Named after the business operation rather than the method, so the span still reads
/// correctly in a dashboard after the method is renamed.
/// </remarks>
public static class ActivityNames
{
    /// <summary>Listing the bookmark collection.</summary>
    public const string ListBookmarks = "Bookmarks.List";

    /// <summary>Reading a single bookmark.</summary>
    public const string GetBookmark = "Bookmarks.Get";

    /// <summary>Creating a bookmark.</summary>
    public const string CreateBookmark = "Bookmarks.Create";

    /// <summary>Replacing a bookmark.</summary>
    public const string UpdateBookmark = "Bookmarks.Update";

    /// <summary>Deleting a bookmark.</summary>
    public const string DeleteBookmark = "Bookmarks.Delete";

    /// <summary>Replacing the set of tags on a bookmark.</summary>
    public const string UpsertBookmarkTags = "Bookmarks.UpsertTags";

    /// <summary>Listing the tag collection.</summary>
    public const string ListTags = "Tags.List";

    /// <summary>Reading a single tag.</summary>
    public const string GetTag = "Tags.Get";

    /// <summary>Creating a tag.</summary>
    public const string CreateTag = "Tags.Create";

    /// <summary>Replacing a tag.</summary>
    public const string UpdateTag = "Tags.Update";

    /// <summary>Deleting a tag.</summary>
    public const string DeleteTag = "Tags.Delete";
}
