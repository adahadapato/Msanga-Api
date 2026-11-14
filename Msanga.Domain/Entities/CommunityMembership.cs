using Msanga.Domain.Enums;

namespace Msanga.Domain.Entities;

/// <summary>
/// "I was a student here" (table CommunityMemberships): a member, a school, the years they were there, and whether
/// they finished there. One per member per school. School first, then the year (decided 14 Nov 2026): this is the
/// circle that sees the school's people, events and memories once confirmed (M3c).
/// </summary>
/// <remarks>
/// <para><b>Your set</b> (<see cref="SetYear"/>, M3b): suggested from your years and the school's naming
/// (<see cref="SuggestedSetYear"/>), then chosen by you, within <see cref="SetYearRange"/>. Null until you choose.
/// A set is a school + a year; there's no set table until something hangs off a set (its chat, its reunion).</para>
/// <para><b>Details</b> (all optional, free text, decided 14 Nov 2026): house, class arm, hostel, department. Tidied
/// later with AI (merges suggested per school, approved by an admin).</para>
/// <para>Leaving deletes the row. Joining again starts afresh (and needs vouching again, M3c).</para>
/// </remarks>
public sealed class CommunityMembership : Entity<Guid>
{
    /// <summary>The earliest year accepted: members are adults, but some finished school long ago.</summary>
    public const int EarliestYear = 1940;

    /// <summary>How far your set's year may be from your years: you left early (your set finished later) or joined
    /// late (your set came in earlier).</summary>
    public const int SetWindowYears = 6;

    /// <summary>Longest house, class arm, hostel or department.</summary>
    public const int DetailMaxLength = 60;

    /// <summary>The longest stay at one school (long courses, repeated years).</summary>
    public const int MaxYearsAtOneSchool = 12;

    /// <summary>Required by EF Core.</summary>
    private CommunityMembership() { }

    private CommunityMembership(Guid memberId, Guid communityId) : base(Guid.CreateVersion7())
    {
        MemberId = memberId;
        CommunityId = communityId;
        Status = CommunityMembershipStatus.Joined;
    }

    /// <summary>A member joins a school.</summary>
    /// <param name="memberId">Who.</param>
    /// <param name="communityId">Which school.</param>
    /// <param name="fromYear">The year they started there.</param>
    /// <param name="toYear">The year they left (or finished).</param>
    /// <param name="finished">True if they finished there.</param>
    /// <param name="currentYear">This year (from the clock), the latest allowed.</param>
    /// <returns>The membership, status Joined.</returns>
    /// <exception cref="ArgumentException">Years that break <see cref="YearsProblem"/>.</exception>
    public static CommunityMembership Join(Guid memberId, Guid communityId, int fromYear, int toYear, bool finished,
                                           int currentYear)
    {
        var problem = YearsProblem(fromYear, toYear, currentYear);
        if (problem is not null) throw new ArgumentException(problem, nameof(fromYear));

        return new CommunityMembership(memberId, communityId) { FromYear = fromYear, ToYear = toYear, Finished = finished };
    }

    /// <summary>The member.</summary>
    public Guid MemberId { get; private set; }

    /// <summary>The school.</summary>
    public Guid CommunityId { get; private set; }

    /// <summary>The school itself (loaded for "My Schools").</summary>
    public Community Community { get; private set; } = null!;

    /// <summary>The year they started there.</summary>
    public int FromYear { get; private set; }

    /// <summary>The year they left or finished.</summary>
    public int ToYear { get; private set; }

    /// <summary>True if they finished there (their home set is then their leaving year's, M3b).</summary>
    public bool Finished { get; private set; }

    /// <summary>Joined, or Confirmed (M3c).</summary>
    public CommunityMembershipStatus Status { get; private set; }

    /// <summary>Your set's year ("1998 Set"); null until you choose it.</summary>
    public int? SetYear { get; private set; }

    /// <summary>Your house (secondary schools), e.g. "Blue House"; null = not given.</summary>
    public string? House { get; private set; }

    /// <summary>Your class arm, e.g. "SS3B"; null = not given.</summary>
    public string? ClassArm { get; private set; }

    /// <summary>Your hostel; null = not given.</summary>
    public string? Hostel { get; private set; }

    /// <summary>Your department (universities, polytechnics), e.g. "Microbiology"; null = not given.</summary>
    public string? Department { get; private set; }

    /// <summary>
    /// The sets your years allow, first to last. By leaving year: from the year you started (a one-year stay) to
    /// <see cref="SetWindowYears"/> after you left (you left early). By entry year: from
    /// <see cref="SetWindowYears"/> before you started (you joined late) to the year you left.
    /// </summary>
    /// <param name="naming">How the school names its sets.</param>
    /// <returns>The first and last allowed set years.</returns>
    public (int First, int Last) SetYearRange(SetNaming naming) => naming == SetNaming.ByEntryYear
        ? (FromYear - SetWindowYears, ToYear)
        : (FromYear, ToYear + SetWindowYears);

    /// <summary>The set your years suggest: by leaving year, the year you left if you finished (null if you left
    /// early: you choose); by entry year, the year you started.</summary>
    /// <param name="naming">How the school names its sets.</param>
    /// <returns>The suggested set year, or null.</returns>
    public int? SuggestedSetYear(SetNaming naming) => naming == SetNaming.ByEntryYear
        ? FromYear
        : Finished ? ToYear : null;

    /// <summary>What's wrong with this set for your years, in words; null if it's fine.</summary>
    /// <param name="setYear">The set's year.</param>
    /// <param name="naming">How the school names its sets.</param>
    /// <returns>The problem, or null.</returns>
    public string? SetYearProblem(int setYear, SetNaming naming)
    {
        var (first, last) = SetYearRange(naming);
        return setYear < first || setYear > last
            ? $"Choose a set between {first} and {last}: those fit the years you were here."
            : null;
    }

    /// <summary>Chooses your set and your details (blank details = not given).</summary>
    /// <param name="setYear">Your set's year.</param>
    /// <param name="naming">How the school names its sets.</param>
    /// <param name="house">House; blank = none.</param>
    /// <param name="classArm">Class arm; blank = none.</param>
    /// <param name="hostel">Hostel; blank = none.</param>
    /// <param name="department">Department; blank = none.</param>
    /// <exception cref="ArgumentException">A set outside <see cref="SetYearRange"/>, or a detail that's too long.</exception>
    public void ChooseSet(int setYear, SetNaming naming, string? house, string? classArm, string? hostel,
                          string? department)
    {
        var problem = SetYearProblem(setYear, naming);
        if (problem is not null) throw new ArgumentException(problem, nameof(setYear));

        SetYear = setYear;
        House = Detail(house, nameof(house));
        ClassArm = Detail(classArm, nameof(classArm));
        Hostel = Detail(hostel, nameof(hostel));
        Department = Detail(department, nameof(department));
    }

    private static string? Detail(string? value, string parameter)
    {
        var text = value?.Trim();
        if (text?.Length > DetailMaxLength)
            throw new ArgumentException($"Keep it to {DetailMaxLength} characters or fewer.", parameter);
        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>Corrects the years (a typo, a forgotten repeated year). The status stays as it is; your set stays
    /// unless it no longer fits the new years (then you choose again).</summary>
    /// <param name="fromYear">The year they started there.</param>
    /// <param name="toYear">The year they left (or finished).</param>
    /// <param name="finished">True if they finished there.</param>
    /// <param name="currentYear">This year (from the clock).</param>
    /// <param name="naming">How the school names its sets.</param>
    /// <exception cref="ArgumentException">Years that break <see cref="YearsProblem"/>.</exception>
    public void ChangeYears(int fromYear, int toYear, bool finished, int currentYear, SetNaming naming)
    {
        var problem = YearsProblem(fromYear, toYear, currentYear);
        if (problem is not null) throw new ArgumentException(problem, nameof(fromYear));

        FromYear = fromYear;
        ToYear = toYear;
        Finished = finished;

        if (SetYear is { } chosen && SetYearProblem(chosen, naming) is not null)
            SetYear = null;
    }

    /// <summary>
    /// What's wrong with these years, in words for the form; null if they're fine. Between <see cref="EarliestYear"/>
    /// and this year, the start no later than the end, at most <see cref="MaxYearsAtOneSchool"/> years.
    /// </summary>
    /// <param name="fromYear">Start year.</param>
    /// <param name="toYear">End year.</param>
    /// <param name="currentYear">This year.</param>
    /// <returns>The problem, or null.</returns>
    public static string? YearsProblem(int fromYear, int toYear, int currentYear)
    {
        if (fromYear < EarliestYear || fromYear > currentYear)
            return $"Choose the year you started, between {EarliestYear} and {currentYear}.";
        if (toYear < EarliestYear || toYear > currentYear)
            return $"Choose the year you left, between {EarliestYear} and {currentYear}.";
        if (toYear < fromYear)
            return "The year you left can't be before the year you started.";
        if (toYear - fromYear + 1 > MaxYearsAtOneSchool)
            return $"That's more than {MaxYearsAtOneSchool} years at one school. Check the years.";
        return null;
    }
}
