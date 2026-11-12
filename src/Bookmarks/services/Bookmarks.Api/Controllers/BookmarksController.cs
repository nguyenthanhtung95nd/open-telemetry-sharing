using Bookmarks.Api.DTOs;
using Bookmarks.Api.Extensions;
using Bookmarks.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bookmarks.Api.Controllers;

/// <summary>HTTP surface for the bookmark collection.</summary>
/// <param name="bookmarkService">Operations on a bookmark's own fields.</param>
/// <param name="bookmarkTagService">Operations on a bookmark's tags.</param>
[ApiController]
[Route("bookmarks")]
[Produces("application/json")]
public sealed class BookmarksController(
    IBookmarkService bookmarkService,
    IBookmarkTagService bookmarkTagService) : ControllerBase
{
    /// <summary>Returns one page of bookmarks.</summary>
    /// <param name="parameters">Filtering, sorting and paging options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 with the page, or 400 when the options are not usable.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] BookmarksQueryParameters parameters, CancellationToken ct)
    {
        var result = await bookmarkService.GetAllAsync(parameters, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult();
    }

    /// <summary>Returns a single bookmark.</summary>
    /// <param name="id">Identifier of the bookmark.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 with the bookmark, or 404.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await bookmarkService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult();
    }

    /// <summary>Creates a bookmark.</summary>
    /// <param name="request">The bookmark to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 with the created bookmark, or 400.</returns>
    [HttpPost]
    public async Task<IActionResult> Create(SaveBookmarkDto request, CancellationToken ct)
    {
        var result = await bookmarkService.CreateAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : result.ToProblemResult();
    }

    /// <summary>Replaces a bookmark's fields.</summary>
    /// <param name="id">Identifier of the bookmark.</param>
    /// <param name="request">The replacement values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 with the updated bookmark, 400, or 404.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, SaveBookmarkDto request, CancellationToken ct)
    {
        var result = await bookmarkService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult();
    }

    /// <summary>Deletes a bookmark.</summary>
    /// <param name="id">Identifier of the bookmark.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204, or 404.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await bookmarkService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult();
    }

    /// <summary>Replaces the complete set of tags attached to a bookmark.</summary>
    /// <param name="id">Identifier of the bookmark.</param>
    /// <param name="request">Identifiers of every tag the bookmark should end up with.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204, 400 when a tag id does not exist, or 404.</returns>
    [HttpPut("{id}/tags")]
    public async Task<IActionResult> UpsertTags(
        string id, UpsertBookmarkTagsDto request, CancellationToken ct)
    {
        var result = await bookmarkTagService.UpsertAsync(id, request, ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult();
    }
}
