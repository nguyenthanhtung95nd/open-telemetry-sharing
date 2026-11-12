using Bookmarks.Api.Common;
using Bookmarks.Api.Domain;
using Bookmarks.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Bookmarks.Api.Tests;

/// <summary>Behaviour of <see cref="Api.Services.BookmarkService"/>.</summary>
public sealed class BookmarkServiceTests
{
    [Fact]
    public async Task GetAllAsync_WhenNoBookmarksMatch_ReturnsEmptyPageNotFailure()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.GetAllAsync(new BookmarksQueryParameters());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_WhenSortFieldIsUnknown_ReturnsValidationFailureNamingTheField()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.GetAllAsync(new BookmarksQueryParameters { Sort = "nonsense desc" });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorKind.Validation, result.ErrorKind);
        Assert.Contains("nonsense", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAllAsync_WhenPageSizeExceedsMaximum_ReturnsValidationFailure()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateBookmarkService(dbContext);
        var parameters = new BookmarksQueryParameters
        {
            PageSize = BookmarksQueryParameters.MaxPageSize + 1
        };

        // Act
        var result = await sut.GetAllAsync(parameters);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task GetAllAsync_WhenPageIsZero_ReturnsValidationFailure()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.GetAllAsync(new BookmarksQueryParameters { Page = 0 });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task GetAllAsync_WhenSortedByTitleDescending_ReturnsItemsInThatOrder()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        await TestContext.SeedBookmarkAsync(dbContext, "Alpha article");
        await TestContext.SeedBookmarkAsync(dbContext, "Zulu article");
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.GetAllAsync(new BookmarksQueryParameters { Sort = "title desc" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Zulu article", result.Value!.Items[0].Title);
        Assert.Equal("Alpha article", result.Value.Items[1].Title);
    }

    [Fact]
    public async Task GetAllAsync_WhenMoreItemsThanPageSize_ReportsTotalAcrossAllPages()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        await TestContext.SeedBookmarkAsync(dbContext, "First article");
        await TestContext.SeedBookmarkAsync(dbContext, "Second article");
        await TestContext.SeedBookmarkAsync(dbContext, "Third article");
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.GetAllAsync(new BookmarksQueryParameters { Page = 1, PageSize = 2 });

        // Assert
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.True(result.Value.HasNextPage);
        Assert.False(result.Value.HasPreviousPage);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookmarkExists_ReturnsSuccess()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var seeded = await TestContext.SeedBookmarkAsync(dbContext, "Findable article");
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.GetByIdAsync(seeded.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Findable article", result.Value!.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookmarkMissing_ReturnsNotFound()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.GetByIdAsync("b_does_not_exist");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorKind.NotFound, result.ErrorKind);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_PersistsTheBookmark()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.CreateAsync(TestContext.ValidBookmark(title: "Saved article"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TestContext.Now.UtcDateTime, result.Value!.CreatedAtUtc);
        Assert.Equal(1, await dbContext.Bookmarks.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenUrlIsNotAbsolute_ReturnsValidationFailureAndPersistsNothing()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.CreateAsync(TestContext.ValidBookmark(url: "/relative/path"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorKind.Validation, result.ErrorKind);
        Assert.Equal(0, await dbContext.Bookmarks.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenTitleIsTooShort_ReturnsValidationFailure()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.CreateAsync(TestContext.ValidBookmark(title: "ab"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task UpdateAsync_WhenBookmarkExists_ReplacesFieldsAndStampsUpdatedAt()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var seeded = await TestContext.SeedBookmarkAsync(dbContext, "Original title");
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.UpdateAsync(seeded.Id, TestContext.ValidBookmark(title: "Replaced title"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Replaced title", result.Value!.Title);
        Assert.Equal(TestContext.Now.UtcDateTime, result.Value.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_WhenBookmarkMissing_ReturnsNotFound()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.UpdateAsync("b_does_not_exist", TestContext.ValidBookmark());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorKind.NotFound, result.ErrorKind);
    }

    [Fact]
    public async Task UpdateAsync_WhenRequestIsInvalid_ReturnsValidationFailureBeforeLookup()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.UpdateAsync("b_does_not_exist", TestContext.ValidBookmark(title: "ab"));

        // Assert
        Assert.Equal(ResultErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task DeleteAsync_WhenBookmarkExists_RemovesIt()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var seeded = await TestContext.SeedBookmarkAsync(dbContext, "Doomed article");
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.DeleteAsync(seeded.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, await dbContext.Bookmarks.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_WhenBookmarkMissing_ReturnsNotFound()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateBookmarkService(dbContext);

        // Act
        var result = await sut.DeleteAsync("b_does_not_exist");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorKind.NotFound, result.ErrorKind);
    }
}
