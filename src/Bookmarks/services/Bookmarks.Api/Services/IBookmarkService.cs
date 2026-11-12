using Bookmarks.Api.Common;
using Bookmarks.Api.DTOs;
using Bookmarks.Api.DTOs.Common;

namespace Bookmarks.Api.Services;

/// <summary>Read and write operations over a bookmark's own fields.</summary>
/// <remarks>
/// Attaching and detaching tags is <see cref="IBookmarkTagService"/>, because that changes
/// the relationship between two entities rather than the bookmark itself.
/// </remarks>
public interface IBookmarkService
{
    /// <summary>Retrieves one page of bookmarks.</summary>
    /// <param name="parameters">Filtering, sorting and paging options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A page of bookmarks, which is empty rather than a failure when nothing matches, or a
    /// validation failure when the options are not usable.
    /// </returns>
    Task<Result<PaginationResult<BookmarkDto>>> GetAllAsync(
        BookmarksQueryParameters parameters, CancellationToken ct = default);

    /// <summary>Retrieves a single bookmark.</summary>
    /// <param name="id">Identifier of the bookmark.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The bookmark, or a not-found failure.</returns>
    Task<Result<BookmarkDto>> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Creates a bookmark.</summary>
    /// <param name="request">The bookmark to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created bookmark, or a validation failure.</returns>
    Task<Result<BookmarkDto>> CreateAsync(SaveBookmarkDto request, CancellationToken ct = default);

    /// <summary>Replaces a bookmark's fields.</summary>
    /// <param name="id">Identifier of the bookmark.</param>
    /// <param name="request">The replacement values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated bookmark, a validation failure, or a not-found failure.</returns>
    Task<Result<BookmarkDto>> UpdateAsync(
        string id, SaveBookmarkDto request, CancellationToken ct = default);

    /// <summary>Deletes a bookmark.</summary>
    /// <param name="id">Identifier of the bookmark.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success, or a not-found failure.</returns>
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
