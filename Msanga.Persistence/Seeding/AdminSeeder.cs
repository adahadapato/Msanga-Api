using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Msanga.Domain.Constants;
using Msanga.Persistence.Configurations.Identity;

namespace Msanga.Persistence.Seeding;

/// <summary>
/// Gives the Admin role to the account in <c>SeedData:AdminEmail</c> (User Secrets), in any environment. Nothing
/// happens if the setting is empty or the account doesn't exist yet: create the account first (sign up), then restart.
/// </summary>
internal static class AdminSeeder
{
    /// <summary>Makes the configured account an admin (once; safe on every start-up).</summary>
    /// <param name="users">Identity's user manager.</param>
    /// <param name="settings">The SeedData settings.</param>
    /// <param name="logger">Start-up log.</param>
    /// <exception cref="InvalidOperationException">Identity refused to add the role.</exception>
    public static async Task SeedAsync(UserManager<ApplicationUser> users, SeedDataSettings settings, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(settings.AdminEmail)) return;

        var user = await users.FindByEmailAsync(settings.AdminEmail.Trim());
        if (user is null)
        {
            logger.LogWarning("SeedData:AdminEmail is set, but there's no account with that email yet. " +
                              "Create the account, then restart the API.");
            return;
        }

        if (await users.IsInRoleAsync(user, Roles.Admin)) return;

        IdentityResultCheck.Ensure(await users.AddToRoleAsync(user, Roles.Admin), "give the admin the Admin role");
        logger.LogInformation("Gave the Admin role to the configured admin account");
    }
}
