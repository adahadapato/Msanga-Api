using Msanga.Domain.Enums;

namespace Msanga.Domain.Entities;

/// <summary>
/// "I was a student here" (table CommunityMemberships): a member, a school, the years they were there, and whether
/// they finished there. One per member per school. School first, then the year (decided 14 Nov 2026): this is the
/// circle that sees the school's people, events and memories once confirmed (M3c).
/// </summary>
/// <remarks>
/// <para><see cref="Finished"/> decides the home set in M3b: finished → the leaving year's set; left early → the
/// member chooses the set they'd have been in.</para>
/// <para>Leaving deletes the row. Joining again starts afresh (and needs vouching again, M3c).</para>
/// </remarks>
public sealed class CommunityMembership : Entity<Guid>
{
    /// <summary>The earliest year accepted: members are adults, but some finished school long ago.</summary>
    public const int EarliestYear = 1940;

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
        var membership = new CommunityMembership(memberId, communityId);
        membership.ChangeYears(fromYear, toYear, finished, currentYear);
        return membership;
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

    /// <summary>Corrects the years (a typo, a forgotten repeated year). The status stays as it is.</summary>
    /// <param name="fromYear">The year they started there.</param>
    /// <param name="toYear">The year they left (or finished).</param>
    /// <param name="finished">True if they finished there.</param>
    /// <param name="currentYear">This year (from the clock).</param>
    /// <exception cref="ArgumentException">Years that break <see cref="YearsProblem"/>.</exception>
    public void ChangeYears(int fromYear, int toYear, bool finished, int currentYear)
    {
        var problem = YearsProblem(fromYear, toYear, currentYear);
        if (problem is not null) throw new ArgumentException(problem, nameof(fromYear));

        FromYear = fromYear;
        ToYear = toYear;
        Finished = finished;
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
