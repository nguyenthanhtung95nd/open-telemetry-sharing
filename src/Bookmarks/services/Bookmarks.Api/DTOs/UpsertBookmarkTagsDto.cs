namespace Bookmarks.Api.DTOs;

/// <summary>The complete set of tags a bookmark should carry after the call.</summary>
/// <param name="TagIds">
/// Identifiers of every tag the bookmark should end up with. Tags not listed are detached,
/// so this replaces the set rather than adding to it.
/// </param>
public sealed record UpsertBookmarkTagsDto(IReadOnlyList<string> TagIds);
