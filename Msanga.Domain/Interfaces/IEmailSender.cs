namespace Msanga.Domain.Interfaces;

/// <summary>
/// Sends emails. Implemented in Infrastructure: SMTP (any provider, settings in <c>Email</c>), or, while
/// <c>Email:Enabled</c> is false, written to a folder so the links can be opened in development.
/// </summary>
/// <remarks>
/// <para><b>Failures throw.</b> Callers that must not fail because of an email (e.g. registration, already saved)
/// catch the exception and log it.</para>
/// <para>Bodies are HTML: callers must HTML-encode anything a person typed (names).</para>
/// </remarks>
public interface IEmailSender
{
    /// <summary>Sends an email.</summary>
    /// <param name="to">The recipient's address.</param>
    /// <param name="toName">The recipient's name.</param>
    /// <param name="subject">The subject line.</param>
    /// <param name="htmlBody">The body as HTML.</param>
    /// <param name="textBody">The same message as plain text (for mail apps that don't show HTML).</param>
    /// <param name="cancellationToken">Cancels sending.</param>
    Task SendAsync(string to, string toName, string subject, string htmlBody, string textBody,
                   CancellationToken cancellationToken = default);
}
