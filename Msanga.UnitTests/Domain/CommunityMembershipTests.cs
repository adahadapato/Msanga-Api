using Msanga.Domain.Entities;
using Msanga.Domain.Enums;

namespace Msanga.UnitTests.Domain;

/// <summary>"I was a student here": the years must make sense.</summary>
public sealed class CommunityMembershipTests
{
    private const int ThisYear = 2026;
    private static readonly Guid Hilary = Guid.CreateVersion7();
    private static readonly Guid Fgcie = Guid.CreateVersion7();

    [Fact]
    public void Joining_keeps_the_years_and_starts_as_Joined()
    {
        var membership = CommunityMembership.Join(Hilary, Fgcie, 1992, 1998, finished: true, ThisYear);

        Assert.Equal(Hilary, membership.MemberId);
        Assert.Equal(Fgcie, membership.CommunityId);
        Assert.Equal((1992, 1998, true), (membership.FromYear, membership.ToYear, membership.Finished));
        Assert.Equal(CommunityMembershipStatus.Joined, membership.Status);
    }

    [Theory]
    [InlineData(1992, 1998)]
    [InlineData(1998, 1998)]          // one year (left early, or a one-year course)
    [InlineData(1940, 1951)]          // the earliest year, 12 years
    [InlineData(2020, ThisYear)]      // left this year
    public void Accepts_sensible_years(int from, int to) =>
        Assert.Null(CommunityMembership.YearsProblem(from, to, ThisYear));

    [Theory]
    [InlineData(1939, 1945, "Choose the year you started, between 1940 and 2026.")]
    [InlineData(2027, 2027, "Choose the year you started, between 1940 and 2026.")]
    [InlineData(2020, 2027, "Choose the year you left, between 1940 and 2026.")]
    [InlineData(1998, 1992, "The year you left can't be before the year you started.")]
    [InlineData(1980, 1992, "That's more than 12 years at one school. Check the years.")]
    public void Refuses_years_that_cannot_be_right(int from, int to, string problem)
    {
        Assert.Equal(problem, CommunityMembership.YearsProblem(from, to, ThisYear));
        Assert.Throws<ArgumentException>(() => CommunityMembership.Join(Hilary, Fgcie, from, to, true, ThisYear));
    }

    [Fact]
    public void Changing_the_years_keeps_the_status()
    {
        var membership = CommunityMembership.Join(Hilary, Fgcie, 1992, 1998, finished: true, ThisYear);

        membership.ChangeYears(1993, 1997, finished: false, ThisYear);

        Assert.Equal((1993, 1997, false), (membership.FromYear, membership.ToYear, membership.Finished));
        Assert.Equal(CommunityMembershipStatus.Joined, membership.Status);
    }
}
