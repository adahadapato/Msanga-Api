namespace Msanga.Persistence.Seeding;

/// <summary>
/// The <c>"SeedData"</c> section (keys in <c>AppSettings.SeedData</c>): demo members for a developer's PC.
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
}
