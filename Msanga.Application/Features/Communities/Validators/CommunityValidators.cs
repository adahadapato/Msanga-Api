using FluentValidation;
using Msanga.Application.Features.Communities.Commands;
using Msanga.Application.Features.Communities.Queries;
using Msanga.Domain.Constants;
using Msanga.Domain.Entities;

namespace Msanga.Application.Features.Communities.Validators;

/// <summary>Checks <see cref="SearchCommunitiesQuery"/>. Blank is fine (no results); very long isn't.</summary>
public sealed class SearchCommunitiesQueryValidator : AbstractValidator<SearchCommunitiesQuery>
{
    /// <summary>The rules.</summary>
    public SearchCommunitiesQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(SearchCommunitiesQuery.SearchMaxLength)
                              .WithMessage($"Keep the search to {SearchCommunitiesQuery.SearchMaxLength} characters or fewer.");
    }
}

/// <summary>Checks <see cref="AddCommunityCommand"/>: the same limits the Community entity guards, plus the state
/// list for Nigeria. Messages are written for the "Add your school" form.</summary>
public sealed class AddCommunityCommandValidator : AbstractValidator<AddCommunityCommand>
{
    /// <summary>The rules.</summary>
    public AddCommunityCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Enter the school's name.")
                            .MaximumLength(Community.NameMaxLength)
                            .WithMessage($"Keep the name to {Community.NameMaxLength} characters or fewer.");

        RuleFor(x => x.Level).IsInEnum().WithMessage("Choose primary, secondary or university / polytechnic.");

        RuleFor(x => x.Town).NotEmpty().WithMessage("Enter the town or city.")
                            .MaximumLength(Community.PlaceMaxLength)
                            .WithMessage($"Keep the town to {Community.PlaceMaxLength} characters or fewer.");

        RuleFor(x => x.CountryCode).NotEmpty().WithMessage("Choose the country.")
                                   .Matches("^[A-Za-z]{2}$").WithMessage("Choose the country.");

        // Nigeria: one of the 36 states or the FCT. Elsewhere: optional, free text.
        RuleFor(x => x.State).Must(state => NigerianStates.Find(state) is not null)
                             .When(x => IsNigeria(x.CountryCode))
                             .WithMessage("Choose the state.");
        RuleFor(x => x.State).MaximumLength(Community.PlaceMaxLength)
                             .When(x => !IsNigeria(x.CountryCode))
                             .WithMessage($"Keep the state or region to {Community.PlaceMaxLength} characters or fewer.");

        RuleFor(x => x.OtherNames).Must(names => names is null || names.Count <= AddCommunityCommand.MaxOtherNames)
                                  .WithMessage($"Add up to {AddCommunityCommand.MaxOtherNames} other names.");
        RuleForEach(x => x.OtherNames).NotEmpty().WithMessage("Enter the other name, or remove it.")
                                      .MaximumLength(Community.NameMaxLength)
                                      .WithMessage($"Keep each other name to {Community.NameMaxLength} characters or fewer.");
    }

    private static bool IsNigeria(string? countryCode) =>
        string.Equals(countryCode?.Trim(), Community.Nigeria, StringComparison.OrdinalIgnoreCase);
}
