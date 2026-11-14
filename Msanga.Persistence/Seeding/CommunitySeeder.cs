using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Msanga.Domain.Entities;
using Msanga.Domain.Enums;

namespace Msanga.Persistence.Seeding;

/// <summary>
/// DEVELOPMENT ONLY (for now): the starting list of schools, so search has something to find on a developer's PC.
/// Runs only when <c>SeedData:CommunitiesEnabled</c> is true (appsettings.Development.json).
/// </summary>
/// <remarks>
/// <para>Safe on every start-up: a school whose key is already in the table is left alone, so schools can be added
/// here later and only the new ones are created.</para>
/// <para>The pilot's real starting list replaces or joins these before the pilot (M9).</para>
/// </remarks>
internal static class CommunitySeeder
{
    private static readonly (string Name, CommunityLevel Level, string Town, string State, string[] OtherNames)[] Schools =
    [
        ("Federal Government College, Ikot Ekpene", CommunityLevel.Secondary, "Ikot Ekpene", "Akwa Ibom",
            ["FGCIE"]),
        ("Government College, Umuahia", CommunityLevel.Secondary, "Umuahia", "Abia", ["GCU"]),
        ("King's College, Lagos", CommunityLevel.Secondary, "Lagos", "Lagos", ["KC", "KC Lagos"]),
        ("University of Uyo", CommunityLevel.Tertiary, "Uyo", "Akwa Ibom", ["UniUyo"]),
        ("University of Lagos", CommunityLevel.Tertiary, "Akoka", "Lagos", ["UNILAG"]),
        ("Akwa Ibom State Polytechnic", CommunityLevel.Tertiary, "Ikot Osurua", "Akwa Ibom", ["Akwapoly", "AKSPOLY"]),
    ];

    /// <summary>Creates the schools that aren't there yet.</summary>
    /// <param name="context">The database.</param>
    /// <param name="logger">Start-up log.</param>
    /// <param name="cancellationToken">Stops seeding.</param>
    public static async Task SeedAsync(MsangaDbContext context, ILogger logger,
                                       CancellationToken cancellationToken = default)
    {
        var added = 0;

        foreach (var school in Schools)
        {
            var community = Community.Create(school.Name, school.Level, school.Town, school.State, Community.Nigeria,
                                             CommunityStatus.Listed, addedByMemberId: null);

            if (await context.Communities.AnyAsync(c => c.SearchKey == community.SearchKey, cancellationToken))
                continue;

            foreach (var otherName in school.OtherNames)
                community.AddAlias(otherName);

            context.Communities.Add(community);
            added++;
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} school(s)", added);
    }
}
