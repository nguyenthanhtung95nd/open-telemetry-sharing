using Bookmarks.Api.Common;
using Bookmarks.Api.Data;
using Bookmarks.Api.Diagnostics;
using Bookmarks.Api.Domain;
using Bookmarks.Api.Extensions;
using Bookmarks.Api.DTOs;
using Bookmarks.Api.Logging;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bookmarks.Api.Services;

/// <summary>Tag operations.</summary>
/// <param name="dbContext">Database context for the bookmarks schema.</param>
/// <param name="validator">Validates tag contents.</param>
/// <param name="timeProvider">Supplies the current time, injected so tests are deterministic.</param>
/// <param name="logger">Logger for this service.</param>
public sealed class TagService(
    AppDbContext dbContext,
    IValidator<SaveTagDto> validator,
    TimeProvider timeProvider,
    ILogger<TagService> logger) : ITagService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TagDto>>> GetAllAsync(CancellationToken ct = default)
    {
        using var operation = ServiceOperation.Start(ActivityNames.ListTags);

        var tags = await dbContext.Tags
            .AsNoTracking()
            .OrderBy(tag => tag.Name)
            .Select(tag => new TagDto(tag.Id, tag.Name, tag.Description, tag.CreatedAtUtc, tag.UpdatedAtUtc))
            .ToListAsync(ct);

        operation.Activity?.SetTag(BookmarkTags.TagCount, tags.Count);
        return Result.Success<IReadOnlyList<TagDto>>(tags);
    }

    /// <inheritdoc />
    public async Task<Result<TagDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        using var operation = ServiceOperation.Start(ActivityNames.GetTag);
        operation.Activity?.SetTag(BookmarkTags.TagId, id);

        var tag = await dbContext.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

        return tag is null
            ? Result.Failure<TagDto>(ResultErrorKind.NotFound, $"Tag '{id}' was not found.")
            : Result.Success(ToDto(tag));
    }

    /// <inheritdoc />
    public async Task<Result<TagDto>> CreateAsync(SaveTagDto request, CancellationToken ct = default)
    {
        using var operation = ServiceOperation.Start(ActivityNames.CreateTag);

        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<TagDto>(ResultErrorKind.Validation, validation.Describe());
        }

        if (await NameTakenAsync(request.Name, excludingId: null, ct))
        {
            logger.TagNameConflict(request.Name);
            return Result.Failure<TagDto>(
                ResultErrorKind.Conflict, $"A tag named '{request.Name}' already exists.");
        }

        var tag = new Tag
        {
            Id = Tag.CreateId(),
            Name = request.Name,
            Description = request.Description,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        dbContext.Tags.Add(tag);
        await dbContext.SaveChangesAsync(ct);

        operation.Activity?.SetTag(BookmarkTags.TagId, tag.Id);
        logger.TagCreated(tag.Id);

        return Result.Success(ToDto(tag));
    }

    /// <inheritdoc />
    public async Task<Result<TagDto>> UpdateAsync(
        string id, SaveTagDto request, CancellationToken ct = default)
    {
        using var operation = ServiceOperation.Start(ActivityNames.UpdateTag);
        operation.Activity?.SetTag(BookmarkTags.TagId, id);

        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<TagDto>(ResultErrorKind.Validation, validation.Describe());
        }

        var tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null)
        {
            return Result.Failure<TagDto>(ResultErrorKind.NotFound, $"Tag '{id}' was not found.");
        }

        if (await NameTakenAsync(request.Name, excludingId: id, ct))
        {
            logger.TagNameConflict(request.Name);
            return Result.Failure<TagDto>(
                ResultErrorKind.Conflict, $"A tag named '{request.Name}' already exists.");
        }

        tag.Name = request.Name;
        tag.Description = request.Description;
        tag.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(ct);

        return Result.Success(ToDto(tag));
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        using var operation = ServiceOperation.Start(ActivityNames.DeleteTag);
        operation.Activity?.SetTag(BookmarkTags.TagId, id);

        var tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null)
        {
            return Result.Failure(ResultErrorKind.NotFound, $"Tag '{id}' was not found.");
        }

        dbContext.Tags.Remove(tag);
        await dbContext.SaveChangesAsync(ct);

        logger.TagDeleted(id);
        return Result.Success();
    }

    private Task<bool> NameTakenAsync(string name, string? excludingId, CancellationToken ct) =>
        dbContext.Tags.AnyAsync(
            tag => tag.Name.ToLower() == name.ToLower() && (excludingId == null || tag.Id != excludingId),
            ct);

    private static TagDto ToDto(Tag tag) =>
        new(tag.Id, tag.Name, tag.Description, tag.CreatedAtUtc, tag.UpdatedAtUtc);
}
