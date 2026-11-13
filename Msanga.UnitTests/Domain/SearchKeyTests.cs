using Msanga.Domain.Shared;

namespace Msanga.UnitTests.Domain;

/// <summary>
/// The search key: different ways of writing a school's name must give the same key, or search and
/// "Did you mean…?" miss the school.
/// </summary>
public sealed class SearchKeyTests
{
    [Theory]
    // Short forms, dots and commas
    [InlineData("Government College, Umuahia", "government college umuahia")]
    [InlineData("Govt. Coll. Umuahia", "government college umuahia")]
    [InlineData("GOV'T COLLEGE UMUAHIA", "government college umuahia")]
    [InlineData("Gov’t College Umuahia", "government college umuahia")]
    // Initials, with or without dots; dashes
    [InlineData("FGC Ikot Ekpene", "federal government college ikot ekpene")]
    [InlineData("F.G.C. Ikot-Ekpene", "federal government college ikot ekpene")]
    [InlineData("Federal Govt. College, Ikot Ekpene", "federal government college ikot ekpene")]
    [InlineData("C.O.E. Afaha Nsit", "college of education afaha nsit")]
    [InlineData("Govt Sec Sch No. 2", "government secondary school no 2")]
    // Apostrophes, "&", "St.", "the"
    [InlineData("King's College, Lagos", "kings college lagos")]
    [InlineData("Kings College Lagos", "kings college lagos")]
    [InlineData("St. Anne's School (Ibadan)", "saint annes school ibadan")]
    [InlineData("Holy Child & St. Mary", "holy child and saint mary")]
    [InlineData("The Polytechnic, Ibadan", "polytechnic ibadan")]
    // Accents and tone marks
    [InlineData("Ọbáfẹ́mi Awólọ́wọ̀ University", "obafemi awolowo university")]
    // Whole words only: these stay as they are
    [InlineData("Uni of Uyo", "university of uyo")]
    [InlineData("UniUyo", "uniuyo")]
    [InlineData("Akwapoly", "akwapoly")]
    [InlineData("Yaba College of Technology", "yaba college of technology")]
    // Spaces
    [InlineData("  Lots   of    spaces  ", "lots of spaces")]
    [InlineData("Ikot Ekpene HS", "ikot ekpene high school")]
    public void Gives_the_same_key_however_the_name_is_written(string name, string expected) =>
        Assert.Equal(expected, SearchKey.For(name));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!! ... ---")]
    public void Blank_or_marks_only_gives_an_empty_key(string? text)
    {
        Assert.Equal("", SearchKey.For(text));
        Assert.Empty(SearchKey.Words(text));
    }

    [Fact]
    public void Typed_search_text_is_split_into_expanded_words()
    {
        string[] expected = ["federal", "government", "college", "ikot"];
        Assert.Equal(expected, SearchKey.Words("fgc ikot"));
    }

    [Fact]
    public void Every_short_form_is_lowercase_and_expands_to_something_longer()
    {
        foreach (var (shortForm, full) in SearchKey.ShortForms)
        {
            Assert.Equal(shortForm.ToLowerInvariant(), shortForm);
            Assert.Equal(full, SearchKey.For(full));            // the full form is already a tidy key
            Assert.True(full.Length > shortForm.Length, $"'{shortForm}' should expand to something longer than itself");
        }
    }
}
