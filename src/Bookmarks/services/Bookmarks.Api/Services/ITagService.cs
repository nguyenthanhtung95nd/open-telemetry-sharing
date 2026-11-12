using Bookmarks.Api.Common;
using Bookmarks.Api.DTOs;

namespace Bookmarks.Api.Services;

/// <summary>Read and write operations over the tag collection.</summary>
public interface ITagService
{
    /// <summary>Retrieves every tag, by name.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The tags, which is an empty collection when none exist.</returns>
    Task<Result<IReadOnlyList<TagDto>>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Retrieves a single tag.</summary>
    /// <param name="id">Identifier of the tag.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The tag, or a not-found failure.</returns>
    Task<Result<TagDto>> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Creates a tag.</summary>
    /// <param name="request">The tag to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The created tag, a validation failure, or a conflict when the name is already taken.
    /// </returns>
    Task<Result<TagDto>> CreateAsync(SaveTagDto request, CancellationToken ct = default);

    /// <summary>Replaces a tag's fields.</summary>
    /// <param name="id">Identifier of the tag.</param>
    /// <param name="request">The replacement values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The updated tag, a validation failure, a not-found failure, or a conflict when the
    /// new name is already taken by another tag.
    /// </returns>
    Task<Result<TagDto>> UpdateAsync(string id, SaveTagDto request, CancellationToken ct = default);

    /// <summary>Deletes a tag, detaching it from every bookmark that carried it.</summary>
    /// <param name="id">Identifier of the tag.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success, or a not-found failure.</returns>
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
