using Bookmarks.Api.Data;
using Bookmarks.Api.Domain;
using Bookmarks.Api.DTOs;
using Bookmarks.Api.Services;
using Bookmarks.Api.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bookmarks.Api.Tests;

/// <summary>
/// Builds a service over an isolated in-memory database.
/// </summary>
/// <remarks>
/// Validators are the real ones rather than substitutes. They are pure logic with no external
/// dependency, so substituting them would only test that the service calls something, not
/// that a bad request is actually refused.
/// </remarks>
internal static class TestContext
{
    /// <summary>A fixed instant so nothing in a test depends on the wall clock.</summary>
    public static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Creates a context backed by a database unique to the calling test.</summary>
    /// <returns>A fresh, empty context.</returns>
    public static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    /// <summary>Creates a bookmark service over <paramref name="dbContext"/>.</summary>
    /// <param name="dbContext">The context the service should use.</param>
    /// <returns>A service wired with real validators and a fixed clock.</returns>
    public static BookmarkService CreateBookmarkService(AppDbContext dbContext) =>
        new(dbContext,
            new BookmarksQueryParametersValidator(),
            new SaveBookmarkDtoValidator(),
            new FixedTimeProvider(Now),
            NullLogger<BookmarkService>.Instance);

    /// <summary>Creates a tag service over <paramref name="dbContext"/>.</summary>
    /// <param name="dbContext">The context the service should use.</param>
    /// <returns>A service wired with real validators and a fixed clock.</returns>
    public static TagService CreateTagService(AppDbContext dbContext) =>
        new(dbContext,
            new SaveTagDtoValidator(),
            new FixedTimeProvider(Now),
            NullLogger<TagService>.Instance);

    /// <summary>Creates a bookmark tag service over <paramref name="dbContext"/>.</summary>
    /// <param name="dbContext">The context the service should use.</param>
    /// <returns>A service ready to use.</returns>
    public static BookmarkTagService CreateBookmarkTagService(AppDbContext dbContext) =>
        new(dbContext, NullLogger<BookmarkTagService>.Instance);

    /// <summary>Builds a valid bookmark request, overriding only what a test cares about.</summary>
    /// <param name="title">Title to use.</param>
    /// <param name="url">Address to use.</param>
    /// <returns>A request that passes validation.</returns>
    public static SaveBookmarkDto ValidBookmark(
        string title = "A perfectly ordinary title",
        string url = "https://example.com/article") =>
        new(title, url, "Saved for later", BookmarkKind.Article, BookmarkStatus.Unread, false);

    /// <summary>Adds a persisted bookmark directly, bypassing the service.</summary>
    /// <param name="dbContext">The context to add to.</param>
    /// <param name="title">Title for the bookmark.</param>
    /// <param name="createdAtUtc">Creation time, to make ordering tests explicit.</param>
    /// <returns>The persisted entity.</returns>
    public static async Task<Bookmark> SeedBookmarkAsync(
        AppDbContext dbContext, string title, DateTime? createdAtUtc = null)
    {
        var bookmark = new Bookmark
        {
            Id = Bookmark.CreateId(),
            Title = title,
            Url = "https://example.com/seeded",
            Kind = BookmarkKind.Article,
            Status = BookmarkStatus.Unread,
            CreatedAtUtc = createdAtUtc ?? Now.UtcDateTime
        };

        dbContext.Bookmarks.Add(bookmark);
        await dbContext.SaveChangesAsync();
        return bookmark;
    }

    /// <summary>Adds a persisted tag directly, bypassing the service.</summary>
    /// <param name="dbContext">The context to add to.</param>
    /// <param name="name">Name for the tag.</param>
    /// <returns>The persisted entity.</returns>
    public static async Task<Tag> SeedTagAsync(AppDbContext dbContext, string name)
    {
        var tag = new Tag { Id = Tag.CreateId(), Name = name, CreatedAtUtc = Now.UtcDateTime };

        dbContext.Tags.Add(tag);
        await dbContext.SaveChangesAsync();
        return tag;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
