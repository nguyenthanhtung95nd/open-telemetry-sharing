using Bookmarks.Api.DTOs;
using FluentValidation;

namespace Bookmarks.Api.Validators;

/// <summary>Rules a tag must satisfy before it is persisted.</summary>
public sealed class SaveTagDtoValidator : AbstractValidator<SaveTagDto>
{
    private const int NameMinLength = 3;
    private const int NameMaxLength = 50;
    private const int DescriptionMaxLength = 50;

    /// <summary>Builds the rule set.</summary>
    public SaveTagDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(NameMinLength, NameMaxLength)
            .WithMessage($"Tag name must be between {NameMinLength} and {NameMaxLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(DescriptionMaxLength)
            .When(x => x.Description is not null)
            .WithMessage($"Tag description cannot exceed {DescriptionMaxLength} characters.");
    }
}
