using FluentValidation;
using Msanga.Application.Features.Communities.Queries;

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
