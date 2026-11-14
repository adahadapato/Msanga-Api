using Msanga.Domain.Entities;
using Msanga.Domain.Enums;

namespace Msanga.Application.Features.Communities.Dtos;

/// <summary>
/// A school, as the app receives it (search results and the school's page). Who added it is never included.
/// </summary>
/// <param name="Id">The school's Id.</param>
/// <param name="Name">Official name.</param>
/// <param name="Kind">"School".</param>
/// <param name="Level">"Primary", "Secondary" or "Tertiary".</param>
/// <param name="Town">Town or city.</param>
/// <param name="State">State or region; null = none.</param>
/// <param name="CountryCode">Two-letter country code, e.g. "NG".</param>
/// <param name="IsNew">Added by a member and not checked yet: show the "new" label.</param>
/// <param name="OtherNames">"Also known as…", A–Z.</param>
/// <param name="SetNaming">"ByLeavingYear" (1992–1998 = 1998 Set) or "ByEntryYear" (= 1992 Set).</param>
public sealed record CommunityDto(
    Guid Id, string Name, CommunityKind Kind, CommunityLevel Level, string Town, string? State,
    string CountryCode, bool IsNew, IReadOnlyList<string> OtherNames, SetNaming SetNaming)
{
    /// <summary>Maps a school (its aliases must be loaded).</summary>
    /// <param name="c">The school.</param>
    /// <returns>The DTO.</returns>
    public static CommunityDto From(Community c) =>
        new(c.Id, c.Name, c.Kind, c.Level, c.Town, c.State, c.CountryCode, c.Status == CommunityStatus.New,
            c.Aliases.Select(a => a.Name).Order(StringComparer.OrdinalIgnoreCase).ToList(), c.SetNaming);
}

/// <summary>One of my schools: the school, my years there, whether I finished, and my status.</summary>
/// <param name="School">The school.</param>
/// <param name="FromYear">The year I started.</param>
/// <param name="ToYear">The year I left.</param>
/// <param name="Finished">True if I finished there.</param>
/// <param name="Status">"Joined", or "Confirmed" (M3c).</param>
/// <param name="SetYear">My set's year; null until I choose it.</param>
/// <param name="SetName">"1998 Set"; null until I choose.</param>
/// <param name="SuggestedSetYear">The set my years suggest (for the form); null if I must choose (left early).</param>
/// <param name="SetYearChoices">The sets my years allow, first to last (for the form's list).</param>
/// <param name="House">House; null = not given.</param>
/// <param name="ClassArm">Class arm ("SS3B"); null = not given.</param>
/// <param name="Hostel">Hostel; null = not given.</param>
/// <param name="Department">Department; null = not given.</param>
/// <param name="VouchedBy">Who said "I know them", oldest first ("Nse U."): "Confirmed by…" (empty if an admin
/// confirmed you, or nobody has vouched yet). Filled by GET …/membership only.</param>
/// <param name="CanVouch">True if I may vouch at this school (confirmed, or the school's early days): show "People
/// waiting". Filled by GET …/membership only.</param>
/// <param name="SetMates">Up to 3 confirmed members of my set besides me, first names ("Utibe", "Nse"), for Home's
/// cards; empty unless I'm confirmed there. Filled by GET /api/me/schools only. Never a number (decided 14 Nov 2026).</param>
/// <param name="MoreSetMates">True if there are more than those ("Utibe, Nse, Ime and others").</param>
public sealed record MembershipDto(
    CommunityDto School, int FromYear, int ToYear, bool Finished, CommunityMembershipStatus Status,
    int? SetYear, string? SetName, int? SuggestedSetYear, IReadOnlyList<int> SetYearChoices,
    string? House, string? ClassArm, string? Hostel, string? Department,
    IReadOnlyList<string> VouchedBy, bool CanVouch, IReadOnlyList<string> SetMates, bool MoreSetMates)
{
    /// <summary>Maps a membership and its school (the school's aliases must be loaded).</summary>
    /// <param name="m">The membership.</param>
    /// <param name="school">Its school.</param>
    /// <param name="vouchedBy">Who vouched (GET …/membership); null = not looked up.</param>
    /// <param name="canVouch">May I vouch here (GET …/membership).</param>
    /// <param name="setMates">A few set-mates' first names (GET /api/me/schools); null = not looked up.</param>
    /// <param name="moreSetMates">More set-mates than those.</param>
    /// <returns>The DTO.</returns>
    public static MembershipDto From(CommunityMembership m, Community school,
                                     IReadOnlyList<string>? vouchedBy = null, bool canVouch = false,
                                     IReadOnlyList<string>? setMates = null, bool moreSetMates = false)
    {
        var (first, last) = m.SetYearRange(school.SetNaming);
        return new(CommunityDto.From(school), m.FromYear, m.ToYear, m.Finished, m.Status,
                   m.SetYear, m.SetYear is { } year ? Community.SetName(year) : null,
                   m.SuggestedSetYear(school.SetNaming), Enumerable.Range(first, last - first + 1).ToList(),
                   m.House, m.ClassArm, m.Hostel, m.Department, vouchedBy ?? [], canVouch, setMates ?? [], moreSetMates);
    }
}

/// <summary>
/// A person at a school, as other members of the school see them (waiting list, a set's members): enough to recognise
/// a classmate, nothing more (decided 14 Nov 2026: no photo, email or other schools).
/// </summary>
/// <param name="MemberId">Their member Id (to vouch for them).</param>
/// <param name="Name">Their name now, "Nse Udo".</param>
/// <param name="SchoolSurname">Their surname at school if different ("Okon"); null if not given.</param>
/// <param name="FromYear">Their first year here.</param>
/// <param name="ToYear">Their last year here.</param>
/// <param name="SetName">"1998 Set"; null if not chosen.</param>
/// <param name="House">House; null if not given.</param>
/// <param name="ClassArm">Class arm; null if not given.</param>
/// <param name="Department">Department; null if not given.</param>
/// <param name="YouVouched">True if I've already said "I know them".</param>
public sealed record PersonDto(
    Guid MemberId, string Name, string? SchoolSurname, int FromYear, int ToYear, string? SetName,
    string? House, string? ClassArm, string? Department, bool YouVouched)
{
    /// <summary>Maps a membership (its Member must be loaded).</summary>
    /// <param name="m">The membership.</param>
    /// <param name="youVouched">Have I vouched for them.</param>
    /// <returns>The DTO.</returns>
    public static PersonDto From(CommunityMembership m, bool youVouched = false) =>
        new(m.MemberId, $"{m.Member.FirstName} {m.Member.LastName}", m.Member.SchoolSurname, m.FromYear, m.ToYear,
            m.SetYear is { } year ? Community.SetName(year) : null, m.House, m.ClassArm, m.Department, youVouched);
}

/// <summary>A set's page: the school and the year. Who's in it comes once members can be confirmed (M3c).</summary>
/// <param name="School">The school.</param>
/// <param name="Year">The set's year.</param>
/// <param name="Name">"1998 Set".</param>
/// <param name="IsMine">True if it's my set.</param>
/// <param name="CanSeeMembers">True if I'm confirmed at the school (only confirmed members see who's in a set).</param>
/// <param name="Members">The set's confirmed members, by name; empty unless <paramref name="CanSeeMembers"/>.</param>
public sealed record SetDto(CommunityDto School, int Year, string Name, bool IsMine, bool CanSeeMembers,
                            IReadOnlyList<PersonDto> Members);

/// <summary>After "I know them": the person, and whether this vouch confirmed them.</summary>
/// <param name="Person">Who.</param>
/// <param name="NowConfirmed">True if they're confirmed now.</param>
public sealed record VouchOutcomeDto(PersonDto Person, bool NowConfirmed);
