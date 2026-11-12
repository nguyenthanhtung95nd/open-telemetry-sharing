using Bookmarks.Api.Common;
using Bookmarks.Api.DTOs;

namespace Bookmarks.Api.Services;

/// <summary>Manages which tags are attached to a bookmark.</summary>
public interface IBookmarkTagService
{
    /// <summary>Replaces the complete set of tags attached to a bookmark.</summary>
    /// <param name="bookmarkId">Identifier of the bookmark.</param>
    /// <param name="request">Identifiers of every tag the bookmark should end up with.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Success, a not-found failure when the bookmark does not exist, or a validation failure
    /// naming the tag identifiers that do not exist. Nothing is changed unless it succeeds.
    /// </returns>
    Task<Result> UpsertAsync(
        string bookmarkId, UpsertBookmarkTagsDto request, CancellationToken ct = default);
}
