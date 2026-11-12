namespace Bookmarks.Api.DTOs;

/// <summary>A tag as returned over the wire.</summary>
/// <param name="Id">Identifier of the tag.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional note about what the tag is for.</param>
/// <param name="CreatedAtUtc">When the tag was created.</param>
/// <param name="UpdatedAtUtc">When the tag was last edited, if ever.</param>
public sealed record TagDto(
    string Id,
    string Name,
    string? Description,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
