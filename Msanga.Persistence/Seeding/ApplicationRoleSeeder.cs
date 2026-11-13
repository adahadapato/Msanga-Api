using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Msanga.Domain.Constants;
using Msanga.Persistence.Configurations.Identity;

namespace Msanga.Persistence.Seeding;

/// <summary>Creates every role in <see cref="Roles.All"/> that doesn't exist yet, with its description.</summary>
/// <remarks>Safe on every start-up: existing roles are left alone, except that a missing description is filled in.</remarks>
internal static class ApplicationRoleSeeder
{
    /// <summary>What each role can do.</summary>
    private static readonly IReadOnlyDictionary<string, string> Descriptions = new Dictionary<string, string>
    {
        [Roles.Member] = "Everyone with an account",
        [Roles.Admin] = "Runs Msanga (moderation and support)"
    };

    /// <summary>Creates the missing roles.</summary>
    /// <param name="roleManager">Identity's role manager.</param>
    /// <param name="logger">Start-up log.</param>
    /// <param name="cancellationToken">Stops seeding.</param>
    /// <exception cref="InvalidOperationException">Identity refused to create or update a role.</exception>
    public static async Task SeedAsync(RoleManager<ApplicationRole> roleManager, ILogger logger,
                                       CancellationToken cancellationToken = default)
    {
        foreach (var roleName in Roles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var description = Descriptions.GetValueOrDefault(roleName);
            var role = await roleManager.FindByNameAsync(roleName);

            if (role is null)
            {
                IdentityResultCheck.Ensure(await roleManager.CreateAsync(new ApplicationRole(roleName, description)),
                                           $"create role '{roleName}'");
                logger.LogInformation("Seeded role {Role}", roleName);
                continue;
            }

            if (role.Description is null && description is not null)
            {
                role.Description = description;
                IdentityResultCheck.Ensure(await roleManager.UpdateAsync(role), $"update role '{roleName}'");
            }
        }
    }
}
