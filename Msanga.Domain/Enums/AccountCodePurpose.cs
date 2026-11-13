namespace Msanga.Domain.Enums;

/// <summary>What an emailed 6-digit code is for (see <c>AccountCode</c>).</summary>
public enum AccountCodePurpose
{
    /// <summary>Confirms a new account's email address (works for 30 minutes by default).</summary>
    ConfirmEmail = 1,

    /// <summary>Lets the owner choose a new password (works for 15 minutes by default).</summary>
    ResetPassword = 2
}
