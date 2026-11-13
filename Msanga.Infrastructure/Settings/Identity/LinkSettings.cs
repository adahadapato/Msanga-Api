using System.ComponentModel.DataAnnotations;
using Msanga.Domain.Constants;

namespace Msanga.Infrastructure.Settings.Identity;

/// <summary>
/// The <c>"Identity:Links"</c> sub-section (keys in <see cref="AppSettings.Identity.Links"/>): how long the emailed
/// links work. The emails and the web app's pages say these times, so keep them in step.
/// </summary>
public sealed class LinkSettings
{
    /// <summary>Confirm-email link lifetime, in hours (1–168, i.e. up to a week).</summary>
    [Range(1, 168)]
    public int ConfirmEmailHours { get; set; } = 24;

    /// <summary>Reset-password link lifetime, in minutes (10–1440).</summary>
    [Range(10, 1440)]
    public int ResetPasswordMinutes { get; set; } = 60;
}
