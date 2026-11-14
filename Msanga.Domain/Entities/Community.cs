using Msanga.Domain.Enums;
using Keys = Msanga.Domain.Shared.SearchKey;

namespace Msanga.Domain.Entities;

/// <summary>
/// A school on Msanga (table Communities): its official name, level, where it is, and its other names
/// (<see cref="Aliases"/>). Its sets (Class of 1998…) come in M3.
/// </summary>
/// <remarks>
/// <para><see cref="SearchKey"/> is the tidy version of the name AND the town (see <see cref="Keys"/>), so
/// "holy child ikoyi" finds Holy Child College in Ikoyi even when the name doesn't say Ikoyi.</para>
/// <para>Who added it is kept for admins (M7) and never shown to other members.</para>
/// </remarks>
public sealed class Community : Entity<Guid>
{
    /// <summary>Longest name (official or other).</summary>
    public const int NameMaxLength = 150;

    /// <summary>Longest town or state.</summary>
    public const int PlaceMaxLength = 100;

    /// <summary>Longest search key: room for the name, the town and written-out short forms.</summary>
    public const int SearchKeyMaxLength = 400;

    /// <summary>Countries are two-letter codes (ISO 3166), e.g. NG, GB, US.</summary>
    public const int CountryCodeLength = 2;

    /// <summary>Nigeria, the country chosen first on every form.</summary>
    public const string Nigeria = "NG";

    private readonly List<CommunityAlias> _aliases = [];

    /// <summary>Required by EF Core.</summary>
    private Community() { }

    private Community(string name, CommunityLevel level, string town, string? state, string countryCode,
                      CommunityStatus status, Guid? addedByMemberId) : base(Guid.CreateVersion7())
    {
        Name = Required(name, NameMaxLength, "Enter the school's name.", nameof(name));
        Kind = CommunityKind.School;
        Level = Enum.IsDefined(level) ? level : throw new ArgumentOutOfRangeException(nameof(level));
        Town = Required(town, PlaceMaxLength, "Enter the town.", nameof(town));
        State = Optional(state, PlaceMaxLength, nameof(state));
        CountryCode = Country(countryCode);
        Status = Enum.IsDefined(status) ? status : throw new ArgumentOutOfRangeException(nameof(status));
        AddedByMemberId = addedByMemberId;

        if (Keys.For(Name).Length == 0)
            throw new ArgumentException("Enter the school's name in letters or numbers.", nameof(name));

        SearchKey = KeyFor(Name, Town);
    }

    /// <summary>Adds a school.</summary>
    /// <param name="name">Official name, e.g. "Federal Government College, Ikot Ekpene".</param>
    /// <param name="level">Primary, secondary or tertiary.</param>
    /// <param name="town">Town or city.</param>
    /// <param name="state">State or region; blank = none.</param>
    /// <param name="countryCode">Two-letter country code, e.g. "NG".</param>
    /// <param name="status">Listed (starting list) or New (added by a member).</param>
    /// <param name="addedByMemberId">The member who added it; null for the starting list.</param>
    /// <returns>The school.</returns>
    /// <exception cref="ArgumentException">A blank or too-long value, or a country that isn't two letters.</exception>
    public static Community Create(string name, CommunityLevel level, string town, string? state,
                                   string countryCode, CommunityStatus status, Guid? addedByMemberId) =>
        new(name, level, town, state, countryCode, status, addedByMemberId);

    /// <summary>The official name, as people should see it.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Always a school, for now.</summary>
    public CommunityKind Kind { get; private set; }

    /// <summary>Primary, secondary or tertiary.</summary>
    public CommunityLevel Level { get; private set; }

    /// <summary>Town or city.</summary>
    public string Town { get; private set; } = string.Empty;

    /// <summary>State or region; null = none.</summary>
    public string? State { get; private set; }

    /// <summary>Two-letter country code, upper case ("NG").</summary>
    public string CountryCode { get; private set; } = Nigeria;

    /// <summary>Listed or New.</summary>
    public CommunityStatus Status { get; private set; }

    /// <summary>Who added it (admins only); null for the starting list.</summary>
    public Guid? AddedByMemberId { get; private set; }

    /// <summary>The name and town as a search key, e.g. <c>government college umuahia</c>.</summary>
    public string SearchKey { get; private set; } = string.Empty;

    /// <summary>Other names people know it by: "FGCIE", "UNILAG".</summary>
    public IReadOnlyCollection<CommunityAlias> Aliases => _aliases;

    /// <summary>Adds another name. One that's only a different way of writing a name it already has (same search
    /// key as the official name or another alias) is skipped.</summary>
    /// <param name="name">The other name, e.g. "FGCIE".</param>
    /// <returns>True if it was added; false if the school already has it.</returns>
    /// <exception cref="ArgumentException">A blank or too-long name.</exception>
    public bool AddAlias(string name)
    {
        var alias = Required(name, NameMaxLength, "Enter the other name.", nameof(name));
        var key = Keys.For(alias);
        if (key.Length == 0)
            throw new ArgumentException("Enter the other name in letters or numbers.", nameof(name));

        if (key == Keys.For(Name) || key == SearchKey || _aliases.Any(a => a.SearchKey == key))
            return false;

        _aliases.Add(new CommunityAlias(Id, alias, key));
        return true;
    }

    /// <summary>The name's key, then the town's, unless the name already has the town in it
    /// ("federal government college ikot ekpene" doesn't get "ikot ekpene" twice).</summary>
    private static string KeyFor(string name, string town)
    {
        var nameKey = Keys.For(name);
        var townKey = Keys.For(town);
        return townKey.Length == 0 || $" {nameKey} ".Contains($" {townKey} ", StringComparison.Ordinal)
            ? nameKey
            : $"{nameKey} {townKey}";
    }

    private static string Required(string value, int maxLength, string whenBlank, string parameter)
    {
        var text = value?.Trim() ?? string.Empty;
        if (text.Length == 0) throw new ArgumentException(whenBlank, parameter);
        if (text.Length > maxLength)
            throw new ArgumentException($"Keep it to {maxLength} characters or fewer.", parameter);
        return text;
    }

    private static string? Optional(string? value, int maxLength, string parameter)
    {
        var text = value?.Trim();
        if (text?.Length > maxLength)
            throw new ArgumentException($"Keep it to {maxLength} characters or fewer.", parameter);
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static string Country(string countryCode)
    {
        var code = countryCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (code.Length != CountryCodeLength || !code.All(char.IsAsciiLetterUpper))
            throw new ArgumentException("Choose a country.", nameof(countryCode));
        return code;
    }
}
