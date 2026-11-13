using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Msanga.Domain.Interfaces;

namespace Msanga.Infrastructure.Settings.Email;

/// <summary>
/// <see cref="IEmailSender"/> used while <c>Email:Enabled</c> is false (development): writes each email as an HTML
/// file to <c>Email:OutboxFolder</c> (default <c>App_Data/emails</c>, next to the app) instead of sending it. Open the
/// newest file in a browser and type its 6-digit code into the page to confirm an email or reset a password.
/// </summary>
/// <remarks>
/// <para>The log says which file was written, but never the code itself (a code is as good as a password while it works).</para>
/// <para><c>App_Data/</c> is in .gitignore, so these files are never committed.</para>
/// </remarks>
internal sealed class OutboxFolderEmailSender(IOptions<EmailSettings> options, TimeProvider clock,
                                              ILogger<OutboxFolderEmailSender> logger) : IEmailSender
{
    /// <inheritdoc />
    public async Task SendAsync(string to, string toName, string subject, string htmlBody, string textBody,
                                CancellationToken cancellationToken = default)
    {
        var folder = Path.GetFullPath(options.Value.OutboxFolder);
        Directory.CreateDirectory(folder);

        var now = clock.GetUtcNow();
        var path = Path.Combine(folder, $"{now:yyyyMMdd-HHmmss-fff}-{Slug(subject)}.html");

        var page = new StringBuilder()
            .Append("<!doctype html><html lang=\"en-GB\"><meta charset=\"utf-8\"><title>")
            .Append(WebUtility.HtmlEncode(subject)).Append("</title>")
            .Append("<p style=\"font:14px system-ui;color:#555;border-bottom:1px solid #ddd;padding-bottom:8px\">")
            .Append("<b>Not sent</b> (Email:Enabled is false). To: ")
            .Append(WebUtility.HtmlEncode($"{toName} <{to}>")).Append(" · Subject: ")
            .Append(WebUtility.HtmlEncode(subject)).Append(" · ").Append(now.ToString("u")).Append("</p>")
            .Append(htmlBody)
            .Append("</html>")
            .ToString();

        await File.WriteAllTextAsync(path, page, Encoding.UTF8, cancellationToken);
        logger.LogInformation("Email (not sent, Email:Enabled is false): {Subject}. Saved to {Path}", subject, path);
    }

    /// <summary>"Confirm your email for Msanga" → "confirm-your-email-for-msanga".</summary>
    private static string Slug(string subject)
    {
        var slug = new string(subject.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-').ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        return slug.Length > 50 ? slug[..50] : slug;
    }
}
