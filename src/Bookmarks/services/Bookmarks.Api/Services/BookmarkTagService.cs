using Bookmarks.Api.Common;
using Bookmarks.Api.Data;
using Bookmarks.Api.Diagnostics;
using Bookmarks.Api.DTOs;
using Bookmarks.Api.Logging;
using Microsoft.EntityFrameworkCore;

namespace Bookmarks.Api.Services;

/// <summary>Attaches and detaches tags on a bookmark.</summary>
/// <param name="dbContext">Database context for the bookmarks schema.</param>
/// <param name="logger">Logger for this service.</param>
public sealed class BookmarkTagService(AppDbContext dbContext, ILogger<BookmarkTagService> logger)
    : IBookmarkTagService
{
    /// <inheritdoc />
    public async Task<Result> UpsertAsync(
        string bookmarkId, UpsertBookmarkTagsDto request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var operation = ServiceOperation.Start(ActivityNames.UpsertBookmarkTags);
        operation.Activity?.SetTag(BookmarkTags.BookmarkId, bookmarkId);

        var bookmark = await dbContext.Bookmarks
            .Include(b => b.Tags)
            .FirstOrDefaultAsync(b => b.Id == bookmarkId, ct);

        if (bookmark is null)
        {
            logger.BookmarkNotFound(bookmarkId);
            return Result.Failure(ResultErrorKind.NotFound, $"Bookmark '{bookmarkId}' was not found.");
        }

        var requestedIds = request.TagIds.Distinct(StringComparer.Ordinal).ToList();
        var tags = await dbContext.Tags.Where(tag => requestedIds.Contains(tag.Id)).ToListAsync(ct);

        // Resolve every tag before changing anything, so a request naming one bad identifier
        // leaves the bookmark exactly as it was rather than half reassigned.
        if (tags.Count != requestedIds.Count)
        {
            var missing = requestedIds.Except(tags.Select(tag => tag.Id), StringComparer.Ordinal);
            return Result.Failure(
                ResultErrorKind.Validation,
                $"These tag ids do not exist: {string.Join(", ", missing)}.");
        }

        bookmark.Tags.Clear();
        bookmark.Tags.AddRange(tags);
        await dbContext.SaveChangesAsync(ct);

        operation.Activity?.SetTag(BookmarkTags.TagCount, tags.Count);
        logger.BookmarkTagsUpserted(bookmarkId, tags.Count);

        return Result.Success();
    }
}
