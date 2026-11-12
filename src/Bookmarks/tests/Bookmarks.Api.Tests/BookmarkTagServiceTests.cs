using Bookmarks.Api.Common;
using Bookmarks.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Bookmarks.Api.Tests;

/// <summary>Behaviour of <see cref="Api.Services.BookmarkTagService"/>.</summary>
public sealed class BookmarkTagServiceTests
{
    [Fact]
    public async Task UpsertAsync_WhenEveryTagExists_ReplacesTheSet()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var bookmark = await TestContext.SeedBookmarkAsync(dbContext, "Tagged article");
        var reading = await TestContext.SeedTagAsync(dbContext, "reading");
        var architecture = await TestContext.SeedTagAsync(dbContext, "architecture");
        var sut = TestContext.CreateBookmarkTagService(dbContext);

        // Act
        var result = await sut.UpsertAsync(
            bookmark.Id, new UpsertBookmarkTagsDto([reading.Id, architecture.Id]));

        // Assert
        Assert.True(result.IsSuccess);
        var stored = await dbContext.Bookmarks.Include(b => b.Tags).SingleAsync();
        Assert.Equal(2, stored.Tags.Count);
    }

    [Fact]
    public async Task UpsertAsync_WhenCalledAgainWithFewerTags_DetachesTheOnesLeftOut()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var bookmark = await TestContext.SeedBookmarkAsync(dbContext, "Tagged article");
        var reading = await TestContext.SeedTagAsync(dbContext, "reading");
        var architecture = await TestContext.SeedTagAsync(dbContext, "architecture");
        var sut = TestContext.CreateBookmarkTagService(dbContext);
        await sut.UpsertAsync(bookmark.Id, new UpsertBookmarkTagsDto([reading.Id, architecture.Id]));

        // Act
        var result = await sut.UpsertAsync(bookmark.Id, new UpsertBookmarkTagsDto([reading.Id]));

        // Assert
        Assert.True(result.IsSuccess);
        var stored = await dbContext.Bookmarks.Include(b => b.Tags).SingleAsync();
        Assert.Equal("reading", Assert.Single(stored.Tags).Name);
    }

    [Fact]
    public async Task UpsertAsync_WhenATagIdIsUnknown_ReturnsValidationFailureAndChangesNothing()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var bookmark = await TestContext.SeedBookmarkAsync(dbContext, "Tagged article");
        var reading = await TestContext.SeedTagAsync(dbContext, "reading");
        var sut = TestContext.CreateBookmarkTagService(dbContext);
        await sut.UpsertAsync(bookmark.Id, new UpsertBookmarkTagsDto([reading.Id]));

        // Act
        var result = await sut.UpsertAsync(
            bookmark.Id, new UpsertBookmarkTagsDto([reading.Id, "t_does_not_exist"]));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorKind.Validation, result.ErrorKind);
        Assert.Contains("t_does_not_exist", result.Error, StringComparison.Ordinal);

        var stored = await dbContext.Bookmarks.Include(b => b.Tags).SingleAsync();
        Assert.Equal("reading", Assert.Single(stored.Tags).Name);
    }

    [Fact]
    public async Task UpsertAsync_WhenBookmarkMissing_ReturnsNotFound()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateBookmarkTagService(dbContext);

        // Act
        var result = await sut.UpsertAsync("b_does_not_exist", new UpsertBookmarkTagsDto([]));

        // Assert
        Assert.Equal(ResultErrorKind.NotFound, result.ErrorKind);
    }

    [Fact]
    public async Task UpsertAsync_WhenGivenAnEmptySet_DetachesEveryTag()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var bookmark = await TestContext.SeedBookmarkAsync(dbContext, "Tagged article");
        var reading = await TestContext.SeedTagAsync(dbContext, "reading");
        var sut = TestContext.CreateBookmarkTagService(dbContext);
        await sut.UpsertAsync(bookmark.Id, new UpsertBookmarkTagsDto([reading.Id]));

        // Act
        var result = await sut.UpsertAsync(bookmark.Id, new UpsertBookmarkTagsDto([]));

        // Assert
        Assert.True(result.IsSuccess);
        var stored = await dbContext.Bookmarks.Include(b => b.Tags).SingleAsync();
        Assert.Empty(stored.Tags);
    }
}
