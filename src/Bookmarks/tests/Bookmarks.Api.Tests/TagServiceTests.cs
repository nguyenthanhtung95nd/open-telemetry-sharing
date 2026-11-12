using Bookmarks.Api.Common;
using Bookmarks.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Bookmarks.Api.Tests;

/// <summary>Behaviour of <see cref="Api.Services.TagService"/>.</summary>
public sealed class TagServiceTests
{
    [Fact]
    public async Task GetAllAsync_WhenNoTagsExist_ReturnsEmptyCollection()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateTagService(dbContext);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetAllAsync_WhenTagsExist_ReturnsThemOrderedByName()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        await TestContext.SeedTagAsync(dbContext, "reading");
        await TestContext.SeedTagAsync(dbContext, "architecture");
        var sut = TestContext.CreateTagService(dbContext);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        Assert.Equal("architecture", result.Value![0].Name);
        Assert.Equal("reading", result.Value[1].Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTagExists_ReturnsSuccess()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var seeded = await TestContext.SeedTagAsync(dbContext, "reading");
        var sut = TestContext.CreateTagService(dbContext);

        // Act
        var result = await sut.GetByIdAsync(seeded.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("reading", result.Value!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTagMissing_ReturnsNotFound()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateTagService(dbContext);

        // Act
        var result = await sut.GetByIdAsync("t_does_not_exist");

        // Assert
        Assert.Equal(ResultErrorKind.NotFound, result.ErrorKind);
    }

    [Fact]
    public async Task CreateAsync_WhenNameIsFree_PersistsTheTag()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateTagService(dbContext);

        // Act
        var result = await sut.CreateAsync(new SaveTagDto("architecture", null));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, await dbContext.Tags.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenNameDiffersOnlyByCase_ReturnsConflict()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        await TestContext.SeedTagAsync(dbContext, "reading");
        var sut = TestContext.CreateTagService(dbContext);

        // Act
        var result = await sut.CreateAsync(new SaveTagDto("Reading", null));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorKind.Conflict, result.ErrorKind);
        Assert.Equal(1, await dbContext.Tags.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenNameIsTooShort_ReturnsValidationFailure()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateTagService(dbContext);

        // Act
        var result = await sut.CreateAsync(new SaveTagDto("ab", null));

        // Assert
        Assert.Equal(ResultErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task UpdateAsync_WhenTagExists_ReplacesFields()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var seeded = await TestContext.SeedTagAsync(dbContext, "reading");
        var sut = TestContext.CreateTagService(dbContext);

        // Act
        var result = await sut.UpdateAsync(seeded.Id, new SaveTagDto("reading list", "Things to read"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("reading list", result.Value!.Name);
        Assert.Equal(TestContext.Now.UtcDateTime, result.Value.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_WhenKeepingItsOwnName_DoesNotReportAConflictWithItself()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var seeded = await TestContext.SeedTagAsync(dbContext, "reading");
        var sut = TestContext.CreateTagService(dbContext);

        // Act
        var result = await sut.UpdateAsync(seeded.Id, new SaveTagDto("reading", "Now described"));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameBelongsToAnotherTag_ReturnsConflict()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        await TestContext.SeedTagAsync(dbContext, "architecture");
        var seeded = await TestContext.SeedTagAsync(dbContext, "reading");
        var sut = TestContext.CreateTagService(dbContext);

        // Act
        var result = await sut.UpdateAsync(seeded.Id, new SaveTagDto("architecture", null));

        // Assert
        Assert.Equal(ResultErrorKind.Conflict, result.ErrorKind);
    }

    [Fact]
    public async Task UpdateAsync_WhenTagMissing_ReturnsNotFound()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateTagService(dbContext);

        // Act
        var result = await sut.UpdateAsync("t_does_not_exist", new SaveTagDto("anything", null));

        // Assert
        Assert.Equal(ResultErrorKind.NotFound, result.ErrorKind);
    }

    [Fact]
    public async Task DeleteAsync_WhenTagExists_RemovesIt()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var seeded = await TestContext.SeedTagAsync(dbContext, "reading");
        var sut = TestContext.CreateTagService(dbContext);

        // Act
        var result = await sut.DeleteAsync(seeded.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, await dbContext.Tags.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_WhenTagMissing_ReturnsNotFound()
    {
        // Arrange
        await using var dbContext = TestContext.CreateDbContext();
        var sut = TestContext.CreateTagService(dbContext);

        // Act
        var result = await sut.DeleteAsync("t_does_not_exist");

        // Assert
        Assert.Equal(ResultErrorKind.NotFound, result.ErrorKind);
    }
}
