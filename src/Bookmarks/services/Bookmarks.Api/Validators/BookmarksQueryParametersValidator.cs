using Bookmarks.Api.DTOs;
using Bookmarks.Api.Services.Sorting;
using FluentValidation;

namespace Bookmarks.Api.Validators;

/// <summary>
/// Rejects collection queries the data layer should never see.
/// </summary>
/// <remarks>
/// Both rules here exist because the implementation this project was adapted from had neither.
/// An unknown sort field surfaced as a 500 from a failed lookup, and an unbounded page size
/// let one request read the whole table.
/// </remarks>
public sealed class BookmarksQueryParametersValidator : AbstractValidator<BookmarksQueryParameters>
{
    /// <summary>Builds the rule set.</summary>
    public BookmarksQueryParametersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, BookmarksQueryParameters.MaxPageSize)
            .WithMessage($"Page size must be between 1 and {BookmarksQueryParameters.MaxPageSize}.");

        RuleFor(x => x.Sort)
            .Must(BookmarkSorting.IsValid)
            .WithMessage(x =>
                $"Unknown sort field '{BookmarkSorting.FieldOf(x.Sort)}'. " +
                $"Allowed fields: {string.Join(", ", BookmarkSorting.AllowedFields)}.");
    }
}
