using Bookmarks.Api.DTOs;
using Bookmarks.Api.Extensions;
using Bookmarks.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bookmarks.Api.Controllers;

/// <summary>HTTP surface for the tag collection.</summary>
/// <param name="tagService">Operations on tags.</param>
[ApiController]
[Route("tags")]
[Produces("application/json")]
public sealed class TagsController(ITagService tagService) : ControllerBase
{
    /// <summary>Returns every tag, by name.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 with the tags, which may be an empty collection.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await tagService.GetAllAsync(ct);
        return Ok(result.Value);
    }

    /// <summary>Returns a single tag.</summary>
    /// <param name="id">Identifier of the tag.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 with the tag, or 404.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await tagService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult();
    }

    /// <summary>Creates a tag.</summary>
    /// <param name="request">The tag to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 with the created tag, 400, or 409 when the name is taken.</returns>
    [HttpPost]
    public async Task<IActionResult> Create(SaveTagDto request, CancellationToken ct)
    {
        var result = await tagService.CreateAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : result.ToProblemResult();
    }

    /// <summary>Replaces a tag's fields.</summary>
    /// <param name="id">Identifier of the tag.</param>
    /// <param name="request">The replacement values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 with the updated tag, 400, 404, or 409.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, SaveTagDto request, CancellationToken ct)
    {
        var result = await tagService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult();
    }

    /// <summary>Deletes a tag, detaching it from every bookmark that carried it.</summary>
    /// <param name="id">Identifier of the tag.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204, or 404.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await tagService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult();
    }
}
