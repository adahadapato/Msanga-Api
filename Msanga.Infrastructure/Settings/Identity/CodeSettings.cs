using System.ComponentModel.DataAnnotations;
using Msanga.Domain.Constants;

namespace Msanga.Infrastructure.Settings.Identity;

/// <summary>
/// The <c>"Identity:Codes"</c> sub-section (keys in <see cref="AppSettings.Identity.Codes"/>): how long the emailed
/// 6-digit codes work. The emails and the web app's pages say these times, so keep them in step. (Wrong tries
/// allowed per code: <c>AccountCode.MaxFailedAttempts</c>, a rule rather than a setting.)
/// </summary>
public sealed class CodeSettings
{
    /// <summary>Confirm-email code lifetime, in minutes (5–1440).</summary>
    [Range(5, 1440)]
    public int ConfirmEmailMinutes { get; set; } = 30;

    /// <summary>Reset-password code lifetime, in minutes (5–60: it gives access to the account, so keep it short).</summary>
    [Range(5, 60)]
    public int ResetPasswordMinutes { get; set; } = 15;
}
