using Bookmarks.Api.Common;
using Bookmarks.Api.Data;
using Bookmarks.Api.Diagnostics;
using Bookmarks.Api.Domain;
using Bookmarks.Api.Extensions;
using Bookmarks.Api.DTOs;
using Bookmarks.Api.DTOs.Common;
using Bookmarks.Api.Logging;
using Bookmarks.Api.Services.Sorting;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bookmarks.Api.Services;

/// <summary>
/// Bookmark operations. This is the layer that starts the service's own spans, because it is
/// the only one that knows which business operation is running.
/// </summary>
/// <param name="dbContext">Database context for the bookmarks schema.</param>
/// <param name="queryValidator">Validates collection query options.</param>
/// <param name="saveValidator">Validates bookmark contents.</param>
/// <param name="timeProvider">Supplies the current time, injected so tests are deterministic.</param>
/// <param name="logger">Logger for this service.</param>
public sealed class BookmarkService(
    AppDbContext dbContext,
    IValidator<BookmarksQueryParameters> queryValidator,
    IValidator<SaveBookmarkDto> saveValidator,
    TimeProvider timeProvider,
    ILogger<BookmarkService> logger) : IBookmarkService
{
    /// <inheritdoc />
    public async Task<Result<PaginationResult<BookmarkDto>>> GetAllAsync(
        BookmarksQueryParameters parameters, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        using var operation = ServiceOperation.Start(ActivityNames.ListBookmarks);

        var validation = await queryValidator.ValidateAsync(parameters, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<PaginationResult<BookmarkDto>>(
                ResultErrorKind.Validation, validation.Describe());
        }

        var query = BookmarkSorting.Apply(Filter(dbContext.Bookmarks.AsNoTracking(), parameters), parameters.Sort);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(BookmarkMappings.Projection)
            .ToListAsync(ct);

        operation.Activity?.SetTag(BookmarkTags.BookmarkCount, items.Count);
        logger.BookmarksListed(items.Count);

        return Result.Success(
            new PaginationResult<BookmarkDto>(items, parameters.Page, parameters.PageSize, totalCount));
    }

    /// <inheritdoc />
    public async Task<Result<BookmarkDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        using var operation = ServiceOperation.Start(ActivityNames.GetBookmark);
        operation.Activity?.SetTag(BookmarkTags.BookmarkId, id);

        var bookmark = await dbContext.Bookmarks
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(BookmarkMappings.Projection)
            .FirstOrDefaultAsync(ct);

        if (bookmark is null)
        {
            logger.BookmarkNotFound(id);
            return Result.Failure<BookmarkDto>(ResultErrorKind.NotFound, $"Bookmark '{id}' was not found.");
        }

        return Result.Success(bookmark);
    }

    /// <inheritdoc />
    public async Task<Result<BookmarkDto>> CreateAsync(
        SaveBookmarkDto request, CancellationToken ct = default)
    {
        using var operation = ServiceOperation.Start(ActivityNames.CreateBookmark);

        var validation = await saveValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<BookmarkDto>(ResultErrorKind.Validation, validation.Describe());
        }

        var bookmark = new Bookmark { Id = Bookmark.CreateId(), CreatedAtUtc = UtcNow() };
        Apply(bookmark, request);

        dbContext.Bookmarks.Add(bookmark);
        await dbContext.SaveChangesAsync(ct);

        operation.Activity?.SetTag(BookmarkTags.BookmarkId, bookmark.Id);
        ApplicationDiagnostics.BookmarksCreatedCounter.Add(1);
        logger.BookmarkCreated(bookmark.Id);

        return Result.Success(BookmarkMappings.ToDto(bookmark));
    }

    /// <inheritdoc />
    public async Task<Result<BookmarkDto>> UpdateAsync(
        string id, SaveBookmarkDto request, CancellationToken ct = default)
    {
        using var operation = ServiceOperation.Start(ActivityNames.UpdateBookmark);
        operation.Activity?.SetTag(BookmarkTags.BookmarkId, id);

        var validation = await saveValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<BookmarkDto>(ResultErrorKind.Validation, validation.Describe());
        }

        var bookmark = await FindWithTagsAsync(id, ct);
        if (bookmark is null)
        {
            logger.BookmarkNotFound(id);
            return Result.Failure<BookmarkDto>(ResultErrorKind.NotFound, $"Bookmark '{id}' was not found.");
        }

        Apply(bookmark, request);
        bookmark.UpdatedAtUtc = UtcNow();
        await dbContext.SaveChangesAsync(ct);

        logger.BookmarkUpdated(bookmark.Id);
        return Result.Success(BookmarkMappings.ToDto(bookmark));
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        using var operation = ServiceOperation.Start(ActivityNames.DeleteBookmark);
        operation.Activity?.SetTag(BookmarkTags.BookmarkId, id);

        var bookmark = await dbContext.Bookmarks.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (bookmark is null)
        {
            logger.BookmarkNotFound(id);
            return Result.Failure(ResultErrorKind.NotFound, $"Bookmark '{id}' was not found.");
        }

        dbContext.Bookmarks.Remove(bookmark);
        await dbContext.SaveChangesAsync(ct);

        logger.BookmarkDeleted(id);
        return Result.Success();
    }

    private Task<Bookmark?> FindWithTagsAsync(string id, CancellationToken ct) =>
        dbContext.Bookmarks.Include(b => b.Tags).FirstOrDefaultAsync(b => b.Id == id, ct);

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;

    private static void Apply(Bookmark bookmark, SaveBookmarkDto request)
    {
        bookmark.Title = request.Title;
        bookmark.Url = request.Url;
        bookmark.Description = request.Description;
        bookmark.Kind = request.Kind;
        bookmark.Status = request.Status;
        bookmark.IsArchived = request.IsArchived;
    }

    private static IQueryable<Bookmark> Filter(
        IQueryable<Bookmark> query, BookmarksQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim();
            query = query.Where(b =>
                EF.Functions.ILike(b.Title, $"%{search}%") ||
                (b.Description != null && EF.Functions.ILike(b.Description, $"%{search}%")));
        }

        if (parameters.Kind is { } kind)
        {
            query = query.Where(b => b.Kind == kind);
        }

        if (parameters.Status is { } status)
        {
            query = query.Where(b => b.Status == status);
        }

        return query;
    }
}
