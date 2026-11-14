using Msanga.Domain.Constants;

namespace Msanga.UnitTests.Domain;

/// <summary>The state list for schools in Nigeria.</summary>
public sealed class NigerianStatesTests
{
    [Fact]
    public void Has_36_states_and_the_FCT_without_repeats()
    {
        Assert.Equal(37, NigerianStates.All.Count);
        Assert.Equal(37, NigerianStates.All.Distinct().Count());
        Assert.Equal(NigerianStates.Fct, NigerianStates.All[^1]);
    }

    [Theory]
    [InlineData("Akwa Ibom", "Akwa Ibom")]
    [InlineData("akwa ibom", "Akwa Ibom")]
    [InlineData("  Akwa   Ibom ", "Akwa Ibom")]
    [InlineData("Lagos State", "Lagos")]
    [InlineData("FCT", NigerianStates.Fct)]
    [InlineData("abuja", NigerianStates.Fct)]
    [InlineData("Federal Capital Territory", NigerianStates.Fct)]
    public void Finds_a_state_however_it_is_written(string sent, string expected) =>
        Assert.Equal(expected, NigerianStates.Find(sent));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Akwa")]
    [InlineData("London")]
    public void Finds_nothing_for_a_place_that_is_not_a_state(string? sent) =>
        Assert.Null(NigerianStates.Find(sent));
}
