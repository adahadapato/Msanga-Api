using FluentValidation;
using Msanga.Application.Features.Communities.Commands;
using Msanga.Application.Features.Communities.Queries;
using Msanga.Domain.Entities;

namespace Msanga.Application.Features.Communities.Validators;

/// <summary>
/// The years at a school, checked for the form (the same rules as <see cref="CommunityMembership.YearsProblem"/>):
/// between 1940 and this year, the start no later than the end, at most 12 years.
/// </summary>
/// <typeparam name="T">Joining, or changing the years.</typeparam>
public abstract class SchoolYearsValidator<T> : AbstractValidator<T> where T : ISchoolYears
{
    /// <summary>The rules.</summary>
    /// <param name="clock">The clock: "this year" is the latest year allowed.</param>
    protected SchoolYearsValidator(TimeProvider clock)
    {
        int ThisYear() => clock.GetUtcNow().Year;

        RuleFor(x => x.FromYear)
            .Must(year => year >= CommunityMembership.EarliestYear && year <= ThisYear())
            .WithMessage(_ => $"Choose the year you started, between {CommunityMembership.EarliestYear} and {ThisYear()}.");

        RuleFor(x => x.ToYear)
            .Must(year => year >= CommunityMembership.EarliestYear && year <= ThisYear())
            .WithMessage(_ => $"Choose the year you left, between {CommunityMembership.EarliestYear} and {ThisYear()}.")
            .DependentRules(() =>
            {
                RuleFor(x => x.ToYear).GreaterThanOrEqualTo(x => x.FromYear)
                    .WithMessage("The year you left can't be before the year you started.");
                RuleFor(x => x.ToYear).Must((x, to) => to - x.FromYear + 1 <= CommunityMembership.MaxYearsAtOneSchool)
                    .When(x => x.ToYear >= x.FromYear)
                    .WithMessage($"That's more than {CommunityMembership.MaxYearsAtOneSchool} years at one school. Check the years.");
            });
    }
}

/// <summary>Checks <see cref="JoinCommunityCommand"/>.</summary>
public sealed class JoinCommunityCommandValidator(TimeProvider clock)
    : SchoolYearsValidator<JoinCommunityCommand>(clock);

/// <summary>Checks <see cref="ChangeMySchoolYearsCommand"/>.</summary>
public sealed class ChangeMySchoolYearsCommandValidator(TimeProvider clock)
    : SchoolYearsValidator<ChangeMySchoolYearsCommand>(clock);

/// <summary>Checks <see cref="ChooseMySetCommand"/>: the details' length. Whether the set fits your years needs your
/// membership, so the handler checks that (400 <c>Community.SetOutOfRange</c>).</summary>
public sealed class ChooseMySetCommandValidator : AbstractValidator<ChooseMySetCommand>
{
    /// <summary>The rules.</summary>
    public ChooseMySetCommandValidator()
    {
        const int max = CommunityMembership.DetailMaxLength;
        RuleFor(x => x.House).MaximumLength(max).WithMessage($"Keep the house to {max} characters or fewer.");
        RuleFor(x => x.ClassArm).MaximumLength(max).WithMessage($"Keep the class arm to {max} characters or fewer.");
        RuleFor(x => x.Hostel).MaximumLength(max).WithMessage($"Keep the hostel to {max} characters or fewer.");
        RuleFor(x => x.Department).MaximumLength(max).WithMessage($"Keep the department to {max} characters or fewer.");
    }
}

/// <summary>Checks <see cref="GetSetQuery"/>: a year from 1940 to a few years from now.</summary>
public sealed class GetSetQueryValidator : AbstractValidator<GetSetQuery>
{
    /// <summary>The rules.</summary>
    /// <param name="clock">The clock ("now").</param>
    public GetSetQueryValidator(TimeProvider clock)
    {
        RuleFor(x => x.Year)
            .Must(year => year >= CommunityMembership.EarliestYear && year <= clock.GetUtcNow().Year + GetSetQuery.YearsAhead)
            .WithMessage("There's no set for that year.");
    }
}
