using System.ComponentModel.DataAnnotations;
using Msanga.Domain.Constants;

namespace Msanga.Infrastructure.Settings.Identity;

/// <summary>
/// The <c>"Identity"</c> section (keys in <see cref="AppSettings.Identity"/>): password, lockout and emailed-code
/// rules.
/// </summary>
public sealed class IdentitySettings
{
    /// <summary>The section's name in appsettings.json.</summary>
    public const string SectionName = AppSettings.Identity.Section;

    /// <summary>How strong a password must be.</summary>
    [Required]
    public PasswordSettings Password { get; set; } = new();

    /// <summary>What happens after too many wrong passwords.</summary>
    [Required]
    public LockoutSettings Lockout { get; set; } = new();

    /// <summary>How long emailed codes work.</summary>
    [Required]
    public CodeSettings Codes { get; set; } = new();
}
