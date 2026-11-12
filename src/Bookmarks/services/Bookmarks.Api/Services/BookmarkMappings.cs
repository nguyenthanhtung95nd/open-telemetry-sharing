using System.Linq.Expressions;
using Bookmarks.Api.Domain;
using Bookmarks.Api.DTOs;

namespace Bookmarks.Api.Services;

/// <summary>Turns bookmark entities into the shape the API returns.</summary>
/// <remarks>
/// The projection exists as an expression so the database can evaluate it, and as a method
/// for entities already in memory. Both live here so the two can never disagree about what a
/// bookmark looks like over the wire.
/// </remarks>
public static class BookmarkMappings
{
    /// <summary>Projection used inside queries, translated to SQL by the provider.</summary>
    public static readonly Expression<Func<Bookmark, BookmarkDto>> Projection = bookmark =>
        new BookmarkDto(
            bookmark.Id,
            bookmark.Title,
            bookmark.Url,
            bookmark.Description,
            bookmark.Kind,
            bookmark.Status,
            bookmark.IsArchived,
            bookmark.Tags.Select(tag => tag.Name).ToList(),
            bookmark.CreatedAtUtc,
            bookmark.UpdatedAtUtc,
            bookmark.LastVisitedAtUtc);

    /// <summary>Maps an entity already loaded into memory.</summary>
    /// <param name="bookmark">The entity to map.</param>
    /// <returns>The wire representation of <paramref name="bookmark"/>.</returns>
    public static BookmarkDto ToDto(Bookmark bookmark)
    {
        ArgumentNullException.ThrowIfNull(bookmark);

        return new BookmarkDto(
            bookmark.Id,
            bookmark.Title,
            bookmark.Url,
            bookmark.Description,
            bookmark.Kind,
            bookmark.Status,
            bookmark.IsArchived,
            bookmark.Tags.Select(tag => tag.Name).ToList(),
            bookmark.CreatedAtUtc,
            bookmark.UpdatedAtUtc,
            bookmark.LastVisitedAtUtc);
    }
}
