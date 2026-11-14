using Msanga.Domain.Entities;

namespace Msanga.UnitTests.Domain;

/// <summary>"Where you live now" (optional, decided 14 Nov 2026): a city needs its country.</summary>
public sealed class WhereYouLiveTests
{
    private static Member Hilary() => Member.Create(Guid.CreateVersion7(), "Hilary", "Adeyemi", null);

    [Fact]
    public void Keeps_a_tidy_city_and_an_upper_case_country()
    {
        var member = Hilary();
        member.SetWhereYouLive("  London ", "gb");
        Assert.Equal(("London", "GB"), (member.LivesInCity, member.LivesInCountryCode));
    }

    [Fact]
    public void A_country_alone_is_fine_and_blanks_clear_it()
    {
        var member = Hilary();
        member.SetWhereYouLive(null, "NG");
        Assert.Null(member.LivesInCity);
        Assert.Equal("NG", member.LivesInCountryCode);

        member.SetWhereYouLive(" ", "");
        Assert.Null(member.LivesInCity);
        Assert.Null(member.LivesInCountryCode);
    }

    [Theory]
    [InlineData("London", null)]      // a city needs its country
    [InlineData("London", "GBR")]
    [InlineData("London", "G1")]
    public void Refuses_a_city_without_a_proper_country(string city, string? country) =>
        Assert.Throws<ArgumentException>(() => Hilary().SetWhereYouLive(city, country));
}
