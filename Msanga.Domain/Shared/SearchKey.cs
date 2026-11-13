using System.Globalization;
using System.Text;

namespace Msanga.Domain.Shared;

/// <summary>
/// Turns a school's name (or what someone typed into the search box) into its <b>search key</b>: a tidy, lowercase
/// version in which two ways of writing the same name come out the same.
/// </summary>
/// <remarks>
/// <para>"Govt. Coll., Umuahia", "GOV'T COLLEGE UMUAHIA" and "Government College Umuahia" all become
/// <c>government college umuahia</c>; "F.G.C. Ikot-Ekpene" becomes <c>federal government college ikot ekpene</c>.</para>
/// <para>The steps, in order:</para>
/// <list type="number">
/// <item>Accents and tone marks go ("Ọbáfẹ́mi" → "obafemi"), and everything is lowercased.</item>
/// <item>Apostrophes are dropped ("King's" → "kings"), "&amp;" becomes "and", and every other mark
/// (dots, commas, dashes, brackets) becomes a space.</item>
/// <item>Initials written with dots or spaces are joined up again ("F.G.C." → "f g c" → "fgc").</item>
/// <item>"the" is dropped ("The Polytechnic, Ibadan" → "polytechnic ibadan").</item>
/// <item>Short forms are written out in full (<see cref="ShortForms"/>): "govt" → "government", "fgc" →
/// "federal government college".</item>
/// </list>
/// <para>Only whole words are expanded, so "Akwapoly" and "UniUyo" stay as they are (they're kept as other names
/// of the school instead).</para>
/// </remarks>
public static class SearchKey
{
    /// <summary>
    /// Short forms people write in Nigerian school names, and what they stand for. Only ones that can't mean
    /// anything else ("tech" could be technical or technology, so it isn't here).
    /// </summary>
    public static IReadOnlyDictionary<string, string> ShortForms { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // Words
        ["govt"] = "government",
        ["gov"] = "government",
        ["fed"] = "federal",
        ["coll"] = "college",
        ["sec"] = "secondary",
        ["sch"] = "school",
        ["schl"] = "school",
        ["pry"] = "primary",
        ["prim"] = "primary",
        ["intl"] = "international",
        ["acad"] = "academy",
        ["univ"] = "university",
        ["uni"] = "university",
        ["poly"] = "polytechnic",
        ["st"] = "saint",
        ["hs"] = "high school",

        // Whole names in a few letters
        ["fgc"] = "federal government college",
        ["fggc"] = "federal government girls college",
        ["gss"] = "government secondary school",
        ["coe"] = "college of education",
    };

    /// <summary>The search key for a name: its <see cref="Words"/> joined by single spaces.</summary>
    /// <param name="text">A school's name, another name for it, or what someone typed.</param>
    /// <returns>The key, e.g. <c>government college umuahia</c>; empty for blank text.</returns>
    public static string For(string? text) => string.Join(' ', Words(text));

    /// <summary>The words of a name's search key, in order.</summary>
    /// <param name="text">A school's name, another name for it, or what someone typed.</param>
    /// <returns>The words, e.g. <c>["federal", "government", "college", "ikot"]</c> for "fgc ikot"; none for blank text.</returns>
    public static IReadOnlyList<string> Words(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var words = new List<string>();

        foreach (var word in JoinInitials(Tidy(text).Split(' ', StringSplitOptions.RemoveEmptyEntries)))
        {
            if (word == "the")
            {
                continue;
            }

            if (ShortForms.TryGetValue(word, out var full))
            {
                words.AddRange(full.Split(' '));
            }
            else
            {
                words.Add(word);
            }
        }

        return words;
    }

    /// <summary>Steps 1 and 2: no accents, lowercase, apostrophes dropped, "&amp;" → "and", other marks → spaces.</summary>
    private static string Tidy(string text)
    {
        // FormD splits "ọ́" into "o" + a dot below + an acute accent; the marks are then skipped.
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var tidy = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            switch (c)
            {
                case '\'' or '’' or '‘' or '`' or 'ʼ': // ' ’ ‘ ` ʼ
                    break;
                case '&':
                    tidy.Append(" and ");
                    break;
                default:
                    tidy.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
                    break;
            }
        }

        return tidy.ToString();
    }

    /// <summary>Step 3: single letters in a row become one word ("f", "g", "c" → "fgc").</summary>
    private static IEnumerable<string> JoinInitials(string[] words)
    {
        var initials = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length == 1 && char.IsLetter(word[0]))
            {
                initials.Append(word);
                continue;
            }

            if (initials.Length > 0)
            {
                yield return initials.ToString();
                initials.Clear();
            }

            yield return word;
        }

        if (initials.Length > 0)
        {
            yield return initials.ToString();
        }
    }
}
