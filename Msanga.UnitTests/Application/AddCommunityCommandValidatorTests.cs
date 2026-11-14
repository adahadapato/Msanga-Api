using Msanga.Application.Features.Communities.Commands;
using Msanga.Application.Features.Communities.Validators;
using Msanga.Domain.Enums;

namespace Msanga.UnitTests.Application;

/// <summary>"Add your school": what the form must send.</summary>
public sealed class AddCommunityCommandValidatorTests
{
    private readonly AddCommunityCommandValidator _validator = new();

    private static AddCommunityCommand School(string state = "Lagos", string country = "NG",
                                              IReadOnlyList<string>? otherNames = null) =>
        new("Holy Child College", CommunityLevel.Secondary, "Ikoyi", state, country, otherNames);

    [Fact]
    public void A_school_in_Nigeria_with_its_state_is_fine() =>
        Assert.True(_validator.Validate(School(otherNames: ["HCC"])).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("Ikoyi")]
    public void A_school_in_Nigeria_needs_one_of_the_states(string state)
    {
        var result = _validator.Validate(School(state));

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(AddCommunityCommand.State), error.PropertyName);
        Assert.Equal("Choose the state.", error.ErrorMessage);
    }

    [Theory]
    [InlineData("Greater London")]
    [InlineData("")]
    public void Elsewhere_the_state_is_free_text_and_optional(string state) =>
        Assert.True(_validator.Validate(School(state, country: "GB")).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("N")]
    [InlineData("NGA")]
    public void Needs_a_two_letter_country(string country) =>
        Assert.Contains(_validator.Validate(School(country: country)).Errors,
                        e => e.PropertyName == nameof(AddCommunityCommand.CountryCode));

    [Fact]
    public void Refuses_more_than_five_other_names() =>
        Assert.Contains(_validator.Validate(School(otherNames: ["A1", "B2", "C3", "D4", "E5", "F6"])).Errors,
                        e => e.PropertyName == nameof(AddCommunityCommand.OtherNames));

    [Fact]
    public void Refuses_a_level_that_does_not_exist() =>
        Assert.Contains(_validator.Validate(School() with { Level = (CommunityLevel)9 }).Errors,
                        e => e.PropertyName == nameof(AddCommunityCommand.Level));
}
