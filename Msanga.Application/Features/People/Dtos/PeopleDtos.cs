using Msanga.Domain.Entities;

namespace Msanga.Application.Features.People.Dtos;

/// <summary>A school you share with someone, as you see it on their card or page.</summary>
/// <param name="SchoolId">The school.</param>
/// <param name="SchoolName">Its name.</param>
/// <param name="SetName">Their set there ("1998 Set"); null if not chosen.</param>
/// <param name="House">Their house; null if not given.</param>
/// <param name="ClassArm">Their class arm; null if not given.</param>
/// <param name="Department">Their department; null if not given.</param>
/// <param name="VouchedBy">"Confirmed by…" there (their page only; empty on cards, or if an admin confirmed them).</param>
public sealed record SharedSchoolDto(
    Guid SchoolId, string SchoolName, string? SetName, string? House, string? ClassArm, string? Department,
    IReadOnlyList<string> VouchedBy)
{
    /// <summary>Maps a confirmed membership (its school must be loaded).</summary>
    /// <param name="m">The membership.</param>
    /// <param name="vouchedBy">Who vouched (their page); null = not looked up.</param>
    /// <returns>The DTO.</returns>
    public static SharedSchoolDto From(CommunityMembership m, IReadOnlyList<string>? vouchedBy = null) =>
        new(m.CommunityId, m.Community.Name, m.SetYear is { } year ? Community.SetName(year) : null, m.House,
            m.ClassArm, m.Department, vouchedBy ?? []);
}

/// <summary>Someone found in Find People: name, where they live, the schools you share. No email, no photo (M4b),
/// no schools you don't share.</summary>
/// <param name="MemberId">Their member Id (their page).</param>
/// <param name="Name">"Nse Udo".</param>
/// <param name="SchoolSurname">Surname at school if different; null if not given.</param>
/// <param name="LivesInCity">Where they live now; null if not given.</param>
/// <param name="LivesInCountryCode">Country they live in now; null if not given.</param>
/// <param name="Schools">The schools you share, A–Z.</param>
public sealed record PersonCardDto(
    Guid MemberId, string Name, string? SchoolSurname, string? LivesInCity, string? LivesInCountryCode,
    IReadOnlyList<SharedSchoolDto> Schools);

/// <summary>A person's page: their card plus their bio, and "Confirmed by" at each school you share.</summary>
/// <param name="MemberId">Their member Id.</param>
/// <param name="Name">"Nse Udo".</param>
/// <param name="SchoolSurname">Surname at school if different; null if not given.</param>
/// <param name="Bio">A line or two about them; empty if none.</param>
/// <param name="LivesInCity">Where they live now; null if not given.</param>
/// <param name="LivesInCountryCode">Country they live in now; null if not given.</param>
/// <param name="Schools">The schools you share, A–Z, with "Confirmed by".</param>
public sealed record PersonPageDto(
    Guid MemberId, string Name, string? SchoolSurname, string Bio, string? LivesInCity, string? LivesInCountryCode,
    IReadOnlyList<SharedSchoolDto> Schools);
