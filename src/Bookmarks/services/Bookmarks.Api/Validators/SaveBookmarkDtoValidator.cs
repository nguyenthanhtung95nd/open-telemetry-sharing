using Bookmarks.Api.Domain;
using Bookmarks.Api.DTOs;
using FluentValidation;

namespace Bookmarks.Api.Validators;

/// <summary>Rules a bookmark must satisfy before it is persisted.</summary>
public sealed class SaveBookmarkDtoValidator : AbstractValidator<SaveBookmarkDto>
{
    private const int TitleMinLength = 3;
    private const int TitleMaxLength = 200;
    private const int UrlMaxLength = 2000;
    private const int DescriptionMaxLength = 500;

    /// <summary>Builds the rule set.</summary>
    public SaveBookmarkDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .Length(TitleMinLength, TitleMaxLength)
            .WithMessage($"Title must be between {TitleMinLength} and {TitleMaxLength} characters.");

        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(UrlMaxLength)
            .WithMessage($"Url cannot exceed {UrlMaxLength} characters.");

        RuleFor(x => x.Url)
            .Must(BeAnAbsoluteWebAddress)
            .WithMessage("Url must be an absolute http or https address.");

        RuleFor(x => x.Description)
            .MaximumLength(DescriptionMaxLength)
            .When(x => x.Description is not null)
            .WithMessage($"Description cannot exceed {DescriptionMaxLength} characters.");

        RuleFor(x => x.Kind)
            .IsInEnum()
            .NotEqual(BookmarkKind.None)
            .WithMessage("Invalid bookmark kind.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .NotEqual(BookmarkStatus.None)
            .WithMessage("Invalid bookmark status.");
    }

    private static bool BeAnAbsoluteWebAddress(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var parsed)
        && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
}
