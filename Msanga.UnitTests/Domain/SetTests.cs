using Msanga.Domain.Entities;
using Msanga.Domain.Enums;

namespace Msanga.UnitTests.Domain;

/// <summary>Your set: suggested from your years and how the school names its sets, then chosen by you.</summary>
public sealed class SetTests
{
    private const int ThisYear = 2026;

    private static CommunityMembership Hilary(int from = 1992, int to = 1998, bool finished = true) =>
        CommunityMembership.Join(Guid.CreateVersion7(), Guid.CreateVersion7(), from, to, finished, ThisYear);

    [Fact]
    public void A_set_is_shown_as_the_year_and_Set() => Assert.Equal("1998 Set", Community.SetName(1998));

    [Fact]
    public void Schools_name_sets_by_leaving_year_unless_told_otherwise()
    {
        var school = Community.Create("Government College", CommunityLevel.Secondary, "Umuahia", "Abia", "NG",
                                      CommunityStatus.Listed, addedByMemberId: null);
        Assert.Equal(SetNaming.ByLeavingYear, school.SetNaming);

        school.ChangeSetNaming(SetNaming.ByEntryYear);
        Assert.Equal(SetNaming.ByEntryYear, school.SetNaming);
    }

    [Theory]
    [InlineData(SetNaming.ByLeavingYear, true, 1998)]    // finished in 1998: the 1998 Set
    [InlineData(SetNaming.ByLeavingYear, false, null)]   // left early: you choose
    [InlineData(SetNaming.ByEntryYear, true, 1992)]      // came in 1992: the 1992 Set
    [InlineData(SetNaming.ByEntryYear, false, 1992)]
    public void Suggests_a_set_from_your_years(SetNaming naming, bool finished, int? suggested) =>
        Assert.Equal(suggested, Hilary(finished: finished).SuggestedSetYear(naming));

    [Theory]
    [InlineData(SetNaming.ByLeavingYear, 1992, 2004)]    // from your first year to 6 after you left
    [InlineData(SetNaming.ByEntryYear, 1986, 1998)]      // from 6 before you came to the year you left
    public void Allows_sets_near_your_years(SetNaming naming, int first, int last) =>
        Assert.Equal((first, last), Hilary().SetYearRange(naming));

    [Fact]
    public void Choosing_a_set_keeps_it_and_tidies_the_details()
    {
        var membership = Hilary();

        membership.ChooseSet(1998, SetNaming.ByLeavingYear, "  Blue House ", "SS3B", "   ", null);

        Assert.Equal(1998, membership.SetYear);
        Assert.Equal("Blue House", membership.House);
        Assert.Equal("SS3B", membership.ClassArm);
        Assert.Null(membership.Hostel);
        Assert.Null(membership.Department);
    }

    [Fact]
    public void Refuses_a_set_that_does_not_fit_your_years()
    {
        var membership = Hilary();

        Assert.Equal("Choose a set between 1992 and 2004: those fit the years you were here.",
                     membership.SetYearProblem(1985, SetNaming.ByLeavingYear));
        Assert.Throws<ArgumentException>(() => membership.ChooseSet(1985, SetNaming.ByLeavingYear, null, null, null, null));
        Assert.Throws<ArgumentException>(() =>
            membership.ChooseSet(1998, SetNaming.ByLeavingYear, new string('a', CommunityMembership.DetailMaxLength + 1), null, null, null));
    }

    [Fact]
    public void Changing_your_years_keeps_your_set_while_it_still_fits()
    {
        var membership = Hilary();
        membership.ChooseSet(1998, SetNaming.ByLeavingYear, "Blue House", null, null, null);

        membership.ChangeYears(1993, 1998, true, ThisYear, SetNaming.ByLeavingYear);
        Assert.Equal(1998, membership.SetYear);

        membership.ChangeYears(2005, 2010, true, ThisYear, SetNaming.ByLeavingYear);   // 1998 no longer fits
        Assert.Null(membership.SetYear);
        Assert.Equal("Blue House", membership.House);                                   // details stay
    }
}
