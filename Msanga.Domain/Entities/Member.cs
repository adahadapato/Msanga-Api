namespace Msanga.Domain.Entities;

/// <summary>
/// A person on Msanga: their name, the surname they had at school, and a line about themselves (table Members).
/// Their sign-in (email, password) is Identity's <c>ApplicationUser</c>, with the SAME Id.
/// </summary>
/// <remarks>
/// <para>No counts of friends, followers or connections live here, on purpose: Msanga isn't a popularity
/// contest.</para>
/// <para>Sets, reunions and memories (M2 onwards) refer to members by this Id.</para>
/// </remarks>
public sealed class Member : Entity<Guid>
{
    /// <summary>Longest first, last or school surname.</summary>
    public const int NameMaxLength = 100;

    /// <summary>Longest "about you".</summary>
    public const int BioMaxLength = 300;

    /// <summary>Longest photo address.</summary>
    public const int PhotoUrlMaxLength = 500;

    /// <summary>Required by EF Core.</summary>
    private Member() { }

    private Member(Guid id, string firstName, string lastName, string? schoolSurname) : base(id)
    {
        SetNames(firstName, lastName, schoolSurname);
    }

    /// <summary>Creates the member for a new account.</summary>
    /// <param name="userId">The new account's Id (the member gets the same one).</param>
    /// <param name="firstName">First name.</param>
    /// <param name="lastName">Last name.</param>
    /// <param name="schoolSurname">Surname at school, if different now; blank = none.</param>
    /// <returns>The member.</returns>
    /// <exception cref="ArgumentException">A blank or too-long name.</exception>
    public static Member Create(Guid userId, string firstName, string lastName, string? schoolSurname) =>
        new(userId, firstName, lastName, schoolSurname);

    /// <summary>First name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Last name.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Their surname at school, if it has changed since (marriage etc.), so classmates can still find them.</summary>
    public string? SchoolSurname { get; private set; }

    /// <summary>A line or two about themselves; empty if none.</summary>
    public string Bio { get; private set; } = string.Empty;

    /// <summary>Their profile picture's address; null = initials. Uploads come later.</summary>
    public string? PhotoUrl { get; private set; }

    /// <summary>"Hilary A.": first name and initial, shown around the app.</summary>
    public string DisplayName => $"{FirstName} {char.ToUpperInvariant(LastName[0])}.";

    /// <summary>Changes the profile.</summary>
    /// <param name="firstName">First name.</param>
    /// <param name="lastName">Last name.</param>
    /// <param name="schoolSurname">Surname at school; blank = none.</param>
    /// <param name="bio">About them; blank = none.</param>
    /// <exception cref="ArgumentException">A blank or too-long value.</exception>
    public void UpdateProfile(string firstName, string lastName, string? schoolSurname, string? bio)
    {
        SetNames(firstName, lastName, schoolSurname);
        var about = (bio ?? string.Empty).Trim();
        if (about.Length > BioMaxLength)
            throw new ArgumentException($"Keep it to {BioMaxLength} characters or fewer.", nameof(bio));
        Bio = about;
    }

    private void SetNames(string firstName, string lastName, string? schoolSurname)
    {
        FirstName = RequiredName(firstName, "Enter your first name.", nameof(firstName));
        LastName = RequiredName(lastName, "Enter your last name.", nameof(lastName));
        var school = schoolSurname?.Trim();
        if (school?.Length > NameMaxLength)
            throw new ArgumentException($"Keep it to {NameMaxLength} characters or fewer.", nameof(schoolSurname));
        SchoolSurname = string.IsNullOrEmpty(school) ? null : school;
    }

    private static string RequiredName(string value, string whenBlank, string parameter)
    {
        var name = value?.Trim() ?? string.Empty;
        if (name.Length == 0) throw new ArgumentException(whenBlank, parameter);
        if (name.Length > NameMaxLength)
            throw new ArgumentException($"Keep it to {NameMaxLength} characters or fewer.", parameter);
        return name;
    }
}
