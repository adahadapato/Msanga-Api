namespace Msanga.Domain.Constants;

/// <summary>
/// Nigeria's 36 states and the Federal Capital Territory: the only "state" values allowed for a school in Nigeria.
/// The web app's form offers the same list (msanga-web/src/data/nigerianStates.ts).
/// </summary>
public static class NigerianStates
{
    /// <summary>The Federal Capital Territory (Abuja).</summary>
    public const string Fct = "Federal Capital Territory";

    /// <summary>All 37, A–Z with the FCT last.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        "Abia", "Adamawa", "Akwa Ibom", "Anambra", "Bauchi", "Bayelsa", "Benue", "Borno", "Cross River", "Delta",
        "Ebonyi", "Edo", "Ekiti", "Enugu", "Gombe", "Imo", "Jigawa", "Kaduna", "Kano", "Katsina", "Kebbi", "Kogi",
        "Kwara", "Lagos", "Nasarawa", "Niger", "Ogun", "Ondo", "Osun", "Oyo", "Plateau", "Rivers", "Sokoto",
        "Taraba", "Yobe", "Zamfara", Fct
    ];

    /// <summary>The state as it's written in <see cref="All"/>: "akwa ibom" → "Akwa Ibom"; "FCT" or "Abuja" → the
    /// FCT; extra spaces and the word "State" are ignored ("Lagos State" → "Lagos").</summary>
    /// <param name="state">What was sent.</param>
    /// <returns>The state's proper name, or null if it isn't one.</returns>
    public static string? Find(string? state)
    {
        var text = string.Join(' ', (state ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (text.EndsWith(" State", StringComparison.OrdinalIgnoreCase))
            text = text[..^" State".Length];

        if (text.Equals("FCT", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("Abuja", StringComparison.OrdinalIgnoreCase))
            return Fct;

        return All.FirstOrDefault(s => s.Equals(text, StringComparison.OrdinalIgnoreCase));
    }
}
