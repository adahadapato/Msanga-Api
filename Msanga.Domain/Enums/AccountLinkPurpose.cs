namespace Msanga.Domain.Enums;

/// <summary>What an emailed account link does (see <c>AccountLink</c>).</summary>
public enum AccountLinkPurpose
{
    /// <summary>Confirms a new account's email address (works for 24 hours by default).</summary>
    ConfirmEmail = 1,

    /// <summary>Lets the owner choose a new password (works for 1 hour by default).</summary>
    ResetPassword = 2
}
