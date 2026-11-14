using Msanga.Application.Features.Communities.Commands;
using Msanga.Application.Features.Communities.Validators;

namespace Msanga.UnitTests.Application;

/// <summary>"Were you a student here?": what the years form must send. The clock is fixed in 2026.</summary>
public sealed class SchoolYearsValidatorTests
{
    /// <summary>A clock stopped on 14 Nov 2026.</summary>
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
    }

    private readonly JoinCommunityCommandValidator _validator = new(new FixedClock());

    private static JoinCommunityCommand Join(int from, int to) => new(Guid.CreateVersion7(), from, to, Finished: true);

    [Fact]
    public void Sensible_years_are_fine() => Assert.True(_validator.Validate(Join(1992, 1998)).IsValid);

    [Fact]
    public void This_year_is_the_latest() =>
        Assert.Equal("Choose the year you left, between 1940 and 2026.",
                     Assert.Single(_validator.Validate(Join(2025, 2027)).Errors).ErrorMessage);

    [Fact]
    public void The_end_cannot_be_before_the_start()
    {
        var error = Assert.Single(_validator.Validate(Join(1998, 1992)).Errors);
        Assert.Equal(nameof(JoinCommunityCommand.ToYear), error.PropertyName);
        Assert.Equal("The year you left can't be before the year you started.", error.ErrorMessage);
    }

    [Fact]
    public void At_most_12_years_at_one_school() =>
        Assert.Equal("That's more than 12 years at one school. Check the years.",
                     Assert.Single(_validator.Validate(Join(1980, 1992)).Errors).ErrorMessage);

    [Fact]
    public void Changing_the_years_has_the_same_rules() =>
        Assert.False(new ChangeMySchoolYearsCommandValidator(new FixedClock())
                         .Validate(new ChangeMySchoolYearsCommand(Guid.CreateVersion7(), 1998, 1992, true)).IsValid);
}
