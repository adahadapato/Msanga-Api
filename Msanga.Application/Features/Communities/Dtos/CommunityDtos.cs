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
public sealed record MembershipDto(
    CommunityDto School, int FromYear, int ToYear, bool Finished, CommunityMembershipStatus Status,
    int? SetYear, string? SetName, int? SuggestedSetYear, IReadOnlyList<int> SetYearChoices,
    string? House, string? ClassArm, string? Hostel, string? Department)
{
    /// <summary>Maps a membership and its school (the school's aliases must be loaded).</summary>
    /// <param name="m">The membership.</param>
    /// <param name="school">Its school.</param>
    /// <returns>The DTO.</returns>
    public static MembershipDto From(CommunityMembership m, Community school)
    {
        var (first, last) = m.SetYearRange(school.SetNaming);
        return new(CommunityDto.From(school), m.FromYear, m.ToYear, m.Finished, m.Status,
                   m.SetYear, m.SetYear is { } year ? Community.SetName(year) : null,
                   m.SuggestedSetYear(school.SetNaming), Enumerable.Range(first, last - first + 1).ToList(),
                   m.House, m.ClassArm, m.Hostel, m.Department);
    }
}

/// <summary>A set's page: the school and the year. Who's in it comes once members can be confirmed (M3c).</summary>
/// <param name="School">The school.</param>
/// <param name="Year">The set's year.</param>
/// <param name="Name">"1998 Set".</param>
/// <param name="IsMine">True if it's my set.</param>
public sealed record SetDto(CommunityDto School, int Year, string Name, bool IsMine);
