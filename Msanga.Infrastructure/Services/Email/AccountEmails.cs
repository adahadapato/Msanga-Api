using System.Net;
using Microsoft.Extensions.Options;
using Msanga.Domain.Interfaces;
using Msanga.Infrastructure.Settings.Identity;

namespace Msanga.Infrastructure.Services.Email;

/// <summary>
/// The wording of Msanga's account emails. They carry a 6-digit code to type into the page, never a link: Msanga
/// never emails sign-in links, so members can treat any email that asks them to click to sign in as fake.
/// </summary>
/// <remarks>
/// <para>Each email is HTML and plain text. The HTML is simple on purpose (one column, real text, the code large and
/// spaced "482 913"), so it reads well in any mail app, with a screen reader and at large text sizes.</para>
/// <para>Names are HTML-encoded: they're typed by people.</para>
/// <para>Failures throw (from <see cref="IEmailSender"/>); <c>AuthService</c> catches and logs them.</para>
/// </remarks>
internal sealed class AccountEmails(IEmailSender sender, IOptions<IdentitySettings> identity)
{
    /// <summary>The line every email ends with.</summary>
    private const string NeverALink = "Msanga will never email you a link to sign in.";

    /// <summary>"Your code to confirm your email": after Create account, and on Send a new code.</summary>
    public Task SendConfirmEmailAsync(string to, string firstName, string code, CancellationToken ct)
    {
        var minutes = identity.Value.Codes.ConfirmEmailMinutes;
        return SendAsync(to, firstName, $"{Spaced(code)} is your Msanga code",
            $"Welcome to Msanga, {firstName}!",
            ["To confirm your email address, type this code on the Msanga page you were on:"],
            code,
            [$"The code works once, for {Lifetime(minutes)}. After 5 wrong tries, ask for a new one."],
            "If you didn't create a Msanga account, you can ignore this email: nothing will happen.",
            ct);
    }

    /// <summary>Someone tried to create an account with an email that already has one (maybe the owner forgot).</summary>
    public Task SendAlreadyRegisteredAsync(string to, string firstName, CancellationToken ct) =>
        SendAsync(to, firstName, "You already have a Msanga account",
            $"Hello {firstName},",
            [
                "Someone (probably you) tried to create a Msanga account with this email address, but it already has one.",
                "You can sign in with it. If you've forgotten your password, choose \"Forgot your password?\" on the sign-in page."
            ],
            code: null, [],
            "If it wasn't you, you can ignore this email: your account hasn't changed.",
            ct);

    /// <summary>"Your code to reset your password": sent from Forgot your password?.</summary>
    public Task SendResetPasswordAsync(string to, string firstName, string code, CancellationToken ct)
    {
        var minutes = identity.Value.Codes.ResetPasswordMinutes;
        return SendAsync(to, firstName, $"{Spaced(code)} is your code to reset your Msanga password",
            $"Hello {firstName},",
            ["We received a request to reset your Msanga password. To choose a new one, type this code on the Msanga page you were on:"],
            code,
            [
                $"The code works once, for {Lifetime(minutes)}. After 5 wrong tries, ask for a new one.",
                "Never give this code to anyone, even someone who says they're from Msanga."
            ],
            "If you didn't ask for this, you can ignore this email: your password stays the same.",
            ct);
    }

    /// <summary>"Your password was changed": after a reset or a change, so a stranger's change doesn't go unnoticed.</summary>
    public Task SendPasswordChangedAsync(string to, string firstName, CancellationToken ct) =>
        SendAsync(to, firstName, "Your Msanga password was changed",
            $"Hello {firstName},",
            [
                "The password for your Msanga account was just changed, and every device was signed out.",
                "If this was you, there's nothing to do."
            ],
            code: null, [],
            "If it wasn't you, reset your password straight away with \"Forgot your password?\" on the Msanga sign-in page.",
            ct);

    // ───────────────────────────── Helpers ─────────────────────────────

    /// <summary>"482913" → "482 913" (easier to read and to type).</summary>
    private static string Spaced(string code) => code.Length == 6 ? $"{code[..3]} {code[3..]}" : code;

    /// <summary>30 → "30 minutes", 60 → "1 hour", 120 → "2 hours".</summary>
    private static string Lifetime(int minutes) =>
        minutes % 60 == 0 ? Plural(minutes / 60, "hour") : Plural(minutes, "minute");

    private static string Plural(int n, string word) => n == 1 ? $"1 {word}" : $"{n} {word}s";

    /// <summary>Builds the HTML and text versions and sends them.</summary>
    private Task SendAsync(string to, string firstName, string subject, string greeting, string[] before,
                           string? code, string[] after, string footer, CancellationToken ct)
    {
        string E(string s) => WebUtility.HtmlEncode(s);
        string Paragraphs(IEnumerable<string> lines) =>
            string.Concat(lines.Select(p => $"<p style=\"margin:0 0 16px\">{E(p)}</p>"));

        var codeHtml = code is null
            ? string.Empty
            : "<p style=\"margin:8px 0 24px;font-size:36px;font-weight:700;letter-spacing:6px;" +
              $"font-family:ui-monospace,Consolas,monospace\">{E(Spaced(code))}</p>";

        var html =
            "<div style=\"max-width:560px;margin:0 auto;padding:24px;font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;" +
            "font-size:16px;line-height:1.6;color:#081b30\">" +
            "<p style=\"margin:0 0 24px;font-size:24px;font-weight:700\">Msanga</p>" +
            $"<h1 style=\"margin:0 0 16px;font-size:22px\">{E(greeting)}</h1>" +
            Paragraphs(before) + codeHtml + Paragraphs(after) +
            $"<p style=\"margin:32px 0 0;color:#4a5072;font-size:14px\">{E(footer)} {E(NeverALink)}</p>" +
            "<p style=\"margin:8px 0 0;color:#4a5072;font-size:14px\">Msanga: mme nsanga, “my friends”.</p>" +
            "</div>";

        var text =
            $"{greeting}\n\n" +
            string.Join("\n\n", before) +
            (code is null ? string.Empty : $"\n\n    {Spaced(code)}") +
            (after.Length == 0 ? string.Empty : "\n\n" + string.Join("\n\n", after)) +
            $"\n\n{footer} {NeverALink}\n\nMsanga: mme nsanga, “my friends”.\n";

        return sender.SendAsync(to, firstName, subject, html, text, ct);
    }
}
