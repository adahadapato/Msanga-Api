using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Msanga.Domain.Constants;
using Msanga.Domain.Entities;
using Msanga.Persistence.Configurations.Identity;

namespace Msanga.Persistence.Seeding;

/// <summary>
/// DEVELOPMENT ONLY: creates the demo member(s) to sign in as, already confirmed, so a fresh database always has
/// someone to try the app with. Runs only when <c>SeedData:DemoMembersEnabled</c> is true (appsettings.Development.json).
/// </summary>
/// <remarks>
/// <para>Safe on every start-up: a demo member who already exists is left alone (so changes you make while testing
/// stay).</para>
/// <para>The password comes from User Secrets (<c>SeedData:DemoPassword</c>), never from the repo. Without it, no demo
/// member is created and the log says how to set it.</para>
/// </remarks>
internal static class DemoMemberSeeder
{
    /// <summary>The demo members: Hilary from the mock-ups, and classmates to try vouching with (M3c).</summary>
    private static readonly (string Email, string FirstName, string LastName, string Bio, string? City, string? Country)[] Demos =
    [
        ("hilary@example.com", "Hilary", "Adeyemi", "Reconnecting with old friends and making new memories.", "Lagos", "NG"),
        ("emeka@example.com", "Emeka", "Obi", "1998 Set. Still the fastest in the 100 m (I think).", null, null),
        ("nse@example.com", "Nse", "Udo", "Red House, 1999 Set. Ask me about the inter-house sports.", "Uyo", "NG"),
        ("utibe@example.com", "Utibe", "Essien", "Blue House, 1998 Set. Living in London these days.", "London", "GB")
    ];

    /// <summary>Demo members already joined at a school (Joined, not confirmed): school name, years, set, house,
    /// class arm. Hilary isn't here: you join with her yourself, then vouch.</summary>
    private static readonly (string Email, string School, int From, int To, int Set, string House, string? ClassArm)[] Memberships =
    [
        ("nse@example.com", "Federal Government College, Ikot Ekpene", 1993, 1999, 1999, "Red House", null),
        ("utibe@example.com", "Federal Government College, Ikot Ekpene", 1992, 1998, 1998, "Blue House", "SS3A")
    ];

    /// <summary>Joins the demo members to their schools (Joined), unless they're there already. Runs after the
    /// schools are seeded.</summary>
    /// <param name="context">The database.</param>
    /// <param name="logger">Start-up log.</param>
    /// <param name="cancellationToken">Stops seeding.</param>
    public static async Task SeedMembershipsAsync(MsangaDbContext context, ILogger logger,
                                                  CancellationToken cancellationToken = default)
    {
        var added = 0;
        foreach (var demo in Memberships)
        {
            var userId = await context.Users.Where(u => u.Email == demo.Email).Select(u => (Guid?)u.Id)
                                      .FirstOrDefaultAsync(cancellationToken);
            var school = await context.Communities.FirstOrDefaultAsync(c => c.Name == demo.School, cancellationToken);
            if (userId is null || school is null) continue;
            if (await context.CommunityMemberships.AnyAsync(m => m.MemberId == userId && m.CommunityId == school.Id,
                                                             cancellationToken)) continue;

            var membership = CommunityMembership.Join(userId.Value, school.Id, demo.From, demo.To, finished: true,
                                                      DateTime.UtcNow.Year);
            membership.ChooseSet(demo.Set, school.SetNaming, demo.House, demo.ClassArm, hostel: null, department: null);
            context.CommunityMemberships.Add(membership);
            added++;
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} demo school membership(s)", added);
    }

    /// <summary>Creates the demo members that don't exist yet.</summary>
    /// <param name="users">Identity's user manager.</param>
    /// <param name="context">The database (for the Member rows).</param>
    /// <param name="settings">The SeedData settings.</param>
    /// <param name="logger">Start-up log.</param>
    /// <param name="cancellationToken">Stops seeding.</param>
    /// <exception cref="InvalidOperationException">Identity refused (e.g. the password breaks the rules).</exception>
    public static async Task SeedAsync(UserManager<ApplicationUser> users, MsangaDbContext context,
                                       SeedDataSettings settings, ILogger logger,
                                       CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.DemoPassword))
        {
            logger.LogWarning(
                "Demo members not created: no password. Set one (at least 10 characters) in Msanga.Presentation: " +
                "dotnet user-secrets set \"SeedData:DemoPassword\" \"your demo password\"");
            return;
        }

        foreach (var demo in Demos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await users.FindByEmailAsync(demo.Email) is { } existing)
            {
                // Already there: only fill in where they live, if it's missing (M3d added it)
                var known = await context.Members.FirstOrDefaultAsync(m => m.Id == existing.Id, cancellationToken);
                if (known is not null && known.LivesInCountryCode is null && demo.Country is not null)
                {
                    known.SetWhereYouLive(demo.City, demo.Country);
                    await context.SaveChangesAsync(cancellationToken);
                }
                continue;
            }

            // Account + Member role + member, all or nothing
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var user = new ApplicationUser { UserName = demo.Email, Email = demo.Email, EmailConfirmed = true };
            IdentityResultCheck.Ensure(await users.CreateAsync(user, settings.DemoPassword),
                                       $"create the demo member {demo.Email}");
            IdentityResultCheck.Ensure(await users.AddToRoleAsync(user, Roles.Member),
                                       $"give {demo.Email} the Member role");

            var member = Member.Create(user.Id, demo.FirstName, demo.LastName, schoolSurname: null);
            member.UpdateProfile(demo.FirstName, demo.LastName, schoolSurname: null, demo.Bio);
            member.SetWhereYouLive(demo.City, demo.Country);
            context.Members.Add(member);
            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Seeded demo member {Email}", demo.Email);
        }
    }
}
