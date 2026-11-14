namespace Msanga.Persistence.Seeding;

/// <summary>
/// The <c>"SeedData"</c> section (keys in <c>AppSettings.SeedData</c>): demo members and schools for a developer's PC.
/// </summary>
/// <remarks>
/// <para>On only in <c>appsettings.Development.json</c>; <c>appsettings.json</c> (production) has it off.</para>
/// <para><see cref="DemoPassword"/> is a SECRET: <c>dotnet user-secrets set "SeedData:DemoPassword" "…"</c>.</para>
/// </remarks>
public sealed class SeedDataSettings
{
    /// <summary>Create the demo members at start-up.</summary>
    public bool DemoMembersEnabled { get; set; }

    /// <summary>The demo members' password; empty = they aren't created (a warning says why).</summary>
    public string DemoPassword { get; set; } = string.Empty;

    /// <summary>Create the starting list of schools at start-up.</summary>
    public bool CommunitiesEnabled { get; set; }

    /// <summary>The account that gets the Admin role at start-up (any environment); empty = nobody. Kept in User Secrets
    /// locally (and a GitHub secret in production, M9): <c>dotnet user-secrets set "SeedData:AdminEmail" "…"</c>.</summary>
    public string AdminEmail { get; set; } = string.Empty;
}
