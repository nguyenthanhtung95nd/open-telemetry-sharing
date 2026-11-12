namespace Bookmarks.Api.Diagnostics;

/// <summary>
/// Span and metric attribute names this service writes. Centralised so a dashboard query
/// never has to guess at spelling.
/// </summary>
/// <remarks>
/// Only identifiers and counts belong here. No request body, header, URL or credential is
/// ever written to a span, because telemetry is retained far longer than the request that
/// produced it and is readable by anyone with dashboard access.
/// </remarks>
public static class BookmarkTags
{
    /// <summary>Number of bookmarks a read operation returned.</summary>
    public const string BookmarkCount = "bookmark.count";

    /// <summary>Identifier of the bookmark an operation acted on.</summary>
    public const string BookmarkId = "bookmark.id";

    /// <summary>Identifier of the tag an operation acted on.</summary>
    public const string TagId = "tag.id";

    /// <summary>Number of tags attached to a bookmark by an upsert.</summary>
    public const string TagCount = "tag.count";

    /// <summary>Name of the service operation, used to tag the duration histogram.</summary>
    public const string Operation = "operation";
}
