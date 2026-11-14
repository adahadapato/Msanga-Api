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
public sealed record CommunityDto(
    Guid Id, string Name, CommunityKind Kind, CommunityLevel Level, string Town, string? State,
    string CountryCode, bool IsNew, IReadOnlyList<string> OtherNames)
{
    /// <summary>Maps a school (its aliases must be loaded).</summary>
    /// <param name="c">The school.</param>
    /// <returns>The DTO.</returns>
    public static CommunityDto From(Community c) =>
        new(c.Id, c.Name, c.Kind, c.Level, c.Town, c.State, c.CountryCode, c.Status == CommunityStatus.New,
            c.Aliases.Select(a => a.Name).Order(StringComparer.OrdinalIgnoreCase).ToList());
}
