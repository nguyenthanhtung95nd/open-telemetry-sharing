namespace Bookmarks.Api.DTOs;

/// <summary>Fields a caller supplies when creating or replacing a tag.</summary>
/// <param name="Name">Display name. Must be unique, ignoring case.</param>
/// <param name="Description">Optional note about what the tag is for.</param>
public sealed record SaveTagDto(string Name, string? Description);
