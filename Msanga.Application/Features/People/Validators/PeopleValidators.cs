using FluentValidation;
using Msanga.Application.Features.People.Queries;
using Msanga.Domain.Entities;

namespace Msanga.Application.Features.People.Validators;

/// <summary>Checks <see cref="FindPeopleQuery"/>: lengths, a two-letter country, a sensible set year.</summary>
public sealed class FindPeopleQueryValidator : AbstractValidator<FindPeopleQuery>
{
    /// <summary>The rules.</summary>
    public FindPeopleQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(FindPeopleQuery.SearchMaxLength)
                              .WithMessage($"Keep the search to {FindPeopleQuery.SearchMaxLength} characters or fewer.");
        RuleFor(x => x.House).MaximumLength(CommunityMembership.DetailMaxLength)
                             .WithMessage($"Keep the house to {CommunityMembership.DetailMaxLength} characters or fewer.");
        RuleFor(x => x.Country).Matches("^[A-Za-z]{2}$").When(x => !string.IsNullOrWhiteSpace(x.Country))
                               .WithMessage("Choose a country.");
        RuleFor(x => x.SetYear).InclusiveBetween(CommunityMembership.EarliestYear, 2100).When(x => x.SetYear is not null)
                               .WithMessage("There's no set for that year.");
    }
}
