using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Msanga.Domain.Interfaces;

namespace Msanga.Infrastructure.Settings.Email;

/// <summary>
/// <see cref="IEmailSender"/> over SMTP (MailKit). One connection per email: account emails are few.
/// </summary>
/// <remarks>Failures are logged and THROWN; callers decide whether an email failing matters (see
/// <see cref="IEmailSender"/>).</remarks>
internal sealed class SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
    : IEmailSender
{
    /// <inheritdoc />
    public async Task SendAsync(string to, string toName, string subject, string htmlBody, string textBody,
                                CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);
        var settings = options.Value;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        if (!string.IsNullOrWhiteSpace(settings.ReplyToAddress))
            message.ReplyTo.Add(new MailboxAddress(settings.FromName, settings.ReplyToAddress));
        message.To.Add(new MailboxAddress(toName, to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody }.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(settings.Host, settings.Port, settings.Security, cancellationToken);

            if (!string.IsNullOrWhiteSpace(settings.Username))
                await client.AuthenticateAsync(settings.Username, settings.Password ?? string.Empty, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            logger.LogInformation("Email sent: {Subject}", subject);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Email failed: {Subject}", subject);
            throw;
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(quit: true, CancellationToken.None);
        }
    }
}
