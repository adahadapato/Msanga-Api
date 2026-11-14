using System.Net;
using Microsoft.Extensions.Logging;
using Msanga.Domain.Interfaces;

namespace Msanga.Application.Features.Communities;

/// <summary>
/// "You're confirmed at FGC Ikot Ekpene": the email sent when someone becomes Confirmed (two vouches, or an admin).
/// Like every Msanga email: no links (Msanga never emails a link to sign in). A failure to send is logged, never
/// undoes the confirmation.
/// </summary>
public sealed class ConfirmedEmail(IAuthService accounts, IEmailSender sender, ILogger<ConfirmedEmail> logger)
{
    /// <summary>Sends it.</summary>
    /// <param name="memberId">Who was confirmed.</param>
    /// <param name="schoolName">Where.</param>
    /// <param name="confirmedBy">Who vouched ("Nse U."); empty if an admin confirmed them.</param>
    /// <param name="ct">Cancels sending.</param>
    public async Task TrySendAsync(Guid memberId, string schoolName, IReadOnlyList<string> confirmedBy,
                                   CancellationToken ct)
    {
        try
        {
            var account = await accounts.GetAccountAsync(memberId, ct);
            if (account.IsFailure) return;

            var (subject, html, text) = Build(account.Value.FirstName, schoolName, confirmedBy);
            await sender.SendAsync(account.Value.Email, account.Value.DisplayName, subject, html, text, ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Couldn't send the confirmed email to member {MemberId}", memberId);
        }
    }

    /// <summary>The email's subject and bodies.</summary>
    /// <param name="firstName">Their first name.</param>
    /// <param name="schoolName">The school.</param>
    /// <param name="confirmedBy">Who vouched; empty = an admin.</param>
    /// <returns>Subject, HTML body, text body.</returns>
    public static (string Subject, string Html, string Text) Build(string firstName, string schoolName,
                                                                   IReadOnlyList<string> confirmedBy)
    {
        var by = confirmedBy.Count == 0 ? "Msanga" : string.Join(" and ", confirmedBy);
        var subject = $"You're confirmed at {schoolName}";
        var text = $"""
            Mme nsanga, {firstName}!

            You're confirmed at {schoolName}. Confirmed by {by}.

            You can now see who else from your school is on Msanga, and say "I know them" for classmates who are
            waiting. Open Msanga to see your set.

            Msanga will never email you a link to sign in.
            """;
        var html = $"""
            <p>Mme nsanga, {WebUtility.HtmlEncode(firstName)}!</p>
            <p>You're confirmed at <strong>{WebUtility.HtmlEncode(schoolName)}</strong>. Confirmed by {WebUtility.HtmlEncode(by)}.</p>
            <p>You can now see who else from your school is on Msanga, and say "I know them" for classmates who are
            waiting. Open Msanga to see your set.</p>
            <p style="color:#555">Msanga will never email you a link to sign in.</p>
            """;
        return (subject, html, text);
    }
}
