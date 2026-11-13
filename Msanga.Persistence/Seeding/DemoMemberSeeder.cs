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
    /// <summary>The demo members: Hilary from the mock-ups. More classmates can be added here later (M2–M6).</summary>
    private static readonly (string Email, string FirstName, string LastName, string Bio)[] Demos =
    [
        ("hilary@example.com", "Hilary", "Adeyemi", "Reconnecting with old friends and making new memories."),
        ("emeka@example.com", "Emeka", "Obi", "Class of 1998. Still the fastest in the 100 m (I think).")
    ];

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
            if (await users.FindByEmailAsync(demo.Email) is not null) continue;

            // Account + Member role + member, all or nothing
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var user = new ApplicationUser { UserName = demo.Email, Email = demo.Email, EmailConfirmed = true };
            IdentityResultCheck.Ensure(await users.CreateAsync(user, settings.DemoPassword),
                                       $"create the demo member {demo.Email}");
            IdentityResultCheck.Ensure(await users.AddToRoleAsync(user, Roles.Member),
                                       $"give {demo.Email} the Member role");

            var member = Member.Create(user.Id, demo.FirstName, demo.LastName, schoolSurname: null);
            member.UpdateProfile(demo.FirstName, demo.LastName, schoolSurname: null, demo.Bio);
            context.Members.Add(member);
            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Seeded demo member {Email}", demo.Email);
        }
    }
}
