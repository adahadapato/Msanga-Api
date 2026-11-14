using Msanga.Domain.Entities;
using Msanga.Domain.Enums;

namespace Msanga.UnitTests.Domain;

/// <summary>A school's rules: its search key includes the town once, other names aren't repeated, places are tidy.</summary>
public sealed class CommunityTests
{
    private static Community School(string name = "Holy Child College", string town = "Ikoyi",
                                    string? state = "Lagos", string country = "NG") =>
        Community.Create(name, CommunityLevel.Secondary, town, state, country, CommunityStatus.Listed,
                         addedByMemberId: null);

    [Fact]
    public void Search_key_adds_the_town_when_the_name_does_not_have_it() =>
        Assert.Equal("holy child college ikoyi", School().SearchKey);

    [Fact]
    public void Search_key_has_the_town_once_when_the_name_already_has_it() =>
        Assert.Equal("federal government college ikot ekpene",
                     School("Federal Government College, Ikot Ekpene", "Ikot Ekpene").SearchKey);

    [Fact]
    public void Other_names_are_added_once_however_they_are_written()
    {
        var school = School("University of Lagos", "Akoka");

        Assert.True(school.AddAlias("UNILAG"));
        Assert.False(school.AddAlias("Unilag"));                  // same key as one it has
        Assert.False(school.AddAlias("Univ. of Lagos"));          // same key as the official name

        var alias = Assert.Single(school.Aliases);
        Assert.Equal("UNILAG", alias.Name);
        Assert.Equal("unilag", alias.SearchKey);
        Assert.Equal(school.Id, alias.CommunityId);
    }

    [Fact]
    public void Text_is_trimmed_the_country_upper_cased_and_a_blank_state_is_none()
    {
        var school = School("  King's College  ", " Lagos ", state: "  ", country: "ng");

        Assert.Equal("King's College", school.Name);
        Assert.Equal("Lagos", school.Town);
        Assert.Null(school.State);
        Assert.Equal("NG", school.CountryCode);
        Assert.Equal(CommunityKind.School, school.Kind);
    }

    [Theory]
    [InlineData("", "Ikoyi", "NG")]          // no name
    [InlineData("!!!", "Ikoyi", "NG")]       // no letters or numbers in the name
    [InlineData("Holy Child", " ", "NG")]    // no town
    [InlineData("Holy Child", "Ikoyi", "")]  // no country
    [InlineData("Holy Child", "Ikoyi", "NGA")]
    [InlineData("Holy Child", "Ikoyi", "N1")]
    public void Refuses_a_school_with_missing_or_wrong_details(string name, string town, string country) =>
        Assert.Throws<ArgumentException>(() => School(name, town, country: country));

    [Fact]
    public void Refuses_a_name_that_is_too_long() =>
        Assert.Throws<ArgumentException>(() => School(new string('a', Community.NameMaxLength + 1)));
}
