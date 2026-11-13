using System.ComponentModel.DataAnnotations;
using MailKit.Security;
using Msanga.Domain.Constants;

namespace Msanga.Infrastructure.Settings.Email;

/// <summary>
/// The <c>"Email"</c> section (keys in <see cref="AppSettings.Email"/>): the SMTP server and who emails come from.
/// Works with any provider that offers SMTP (Brevo, Gmail / Google Workspace, Zoho, Microsoft 365, your host's mail).
/// </summary>
/// <remarks>
/// <para>When <see cref="Enabled"/> is false (development), emails are written as files to <see cref="OutboxFolder"/>
/// instead, so the confirm and reset codes can be read without a mail server.</para>
/// <para><see cref="Password"/> is a SECRET: User Secrets in development, environment variables in production.</para>
/// </remarks>
public sealed class EmailSettings
{
    /// <summary>The section's name in appsettings.json.</summary>
    public const string SectionName = AppSettings.Email.Section;

    /// <summary>Send real emails (true) or write them to <see cref="OutboxFolder"/> (false).</summary>
    public bool Enabled { get; set; }

    /// <summary>The name emails come from, e.g. "Msanga".</summary>
    [Required]
    public string FromName { get; set; } = string.Empty;

    /// <summary>The address emails come from (must be allowed by the SMTP provider).</summary>
    [Required, EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Where replies go, e.g. a support inbox; empty = <see cref="FromAddress"/>.</summary>
    public string? ReplyToAddress { get; set; }

    /// <summary>The SMTP server, e.g. "smtp-relay.brevo.com".</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>The SMTP port (587 for STARTTLS, 465 for SSL).</summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    /// <summary>How the connection is secured.</summary>
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

    /// <summary>SMTP user name; empty for none.</summary>
    public string? Username { get; set; }

    /// <summary>SMTP password or API key (SECRET).</summary>
    public string? Password { get; set; }

    /// <summary>While <see cref="Enabled"/> is false: where emails are written, relative to the app's folder.</summary>
    public string OutboxFolder { get; set; } = "App_Data/emails";
}
