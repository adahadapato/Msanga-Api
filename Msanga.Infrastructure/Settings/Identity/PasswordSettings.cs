using System.ComponentModel.DataAnnotations;
using Msanga.Domain.Constants;

namespace Msanga.Infrastructure.Settings.Identity;

/// <summary>
/// The <c>"Identity:Password"</c> sub-section (keys in <see cref="AppSettings.Identity.Password"/>): how strong a
/// password must be when an account is created or its password changed or reset.
/// </summary>
public sealed class PasswordSettings
{
    /// <summary>Minimum length (6–128).</summary>
    [Range(6, 128)]
    public int RequiredLength { get; set; } = 10;

    /// <summary>Needs a digit 0–9.</summary>
    public bool RequireDigit { get; set; }

    /// <summary>Needs a lowercase letter.</summary>
    public bool RequireLowercase { get; set; }

    /// <summary>Needs an uppercase letter.</summary>
    public bool RequireUppercase { get; set; }

    /// <summary>Needs a symbol such as ! or #.</summary>
    public bool RequireNonAlphanumeric { get; set; }

    /// <summary>Minimum number of different characters (1–128).</summary>
    [Range(1, 128)]
    public int RequiredUniqueChars { get; set; } = 1;
}