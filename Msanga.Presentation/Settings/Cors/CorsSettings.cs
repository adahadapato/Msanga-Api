using Msanga.Domain.Constants;

namespace Msanga.Presentation.Settings.Cors;

/// <summary>
/// The <c>Cors</c> section: which web apps may call the API from a browser. Keys: <see cref="AppSettings.Cors"/>.
/// </summary>
/// <remarks>
/// <para>Production lists the Msanga web app (<c>https://msanga.app</c>, with and without www); development
/// lists the local Vite address (<c>http://localhost:5174</c>).</para>
/// <para>Checked when the app starts (see <see cref="CorsSetup"/>): a malformed address stops the app instead of
/// silently blocking the web app.</para>
/// </remarks>
public sealed class CorsSettings
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = AppSettings.Cors.Section;

    /// <summary>Full addresses, e.g. "https://msanga.app": scheme + host (+ port), no path, no trailing
    /// slash. Empty = no web app may call the API from a browser.</summary>
    public string[] AllowedOrigins { get; set; } = [];
}
