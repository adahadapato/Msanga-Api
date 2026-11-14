namespace Msanga.Domain.Shared.Errors;

/// <summary>Failures about communities (schools).</summary>
public static class CommunityErrors
{
    /// <summary>No school with that Id (or it was removed).</summary>
    public static readonly Error NotFound = Error.NotFound(
        "Community.NotFound", "We couldn't find that school.");

    /// <summary>Joining a school you've already joined (change your years instead).</summary>
    public static readonly Error AlreadyMember = Error.Conflict(
        "Community.AlreadyMember", "You've already joined this school. You can change your years instead.");

    /// <summary>Changing or leaving a school you haven't joined.</summary>
    public static readonly Error NotAMember = Error.NotFound(
        "Community.NotAMember", "You haven't joined this school.");

    /// <summary>A school's sets and people are only for those who were students there (school first).</summary>
    public static readonly Error MembersOnly = Error.Forbidden(
        "Community.MembersOnly", "Only people who were students here can see this. Join the school first.");

    /// <summary>Choosing a set that doesn't fit your years at the school. Built with the allowed range, so it's
    /// made by <see cref="SetOutOfRange"/>.</summary>
    /// <param name="message">"Choose a set between 1992 and 2004: those fit the years you were here."</param>
    /// <returns>The error (400).</returns>
    public static Error SetOutOfRange(string message) => Error.Validation("Community.SetOutOfRange", message);

    /// <summary>Vouching: there's no such person at this school.</summary>
    public static readonly Error PersonNotFound = Error.NotFound(
        "Community.PersonNotFound", "We couldn't find that person at this school.");

    /// <summary>Vouching for yourself.</summary>
    public static readonly Error CannotVouchForYourself = Error.Validation(
        "Community.CannotVouchForYourself", "You can't vouch for yourself.");

    /// <summary>Vouching (or seeing who's waiting) before you're confirmed, once the school's early days are over.</summary>
    public static readonly Error NotConfirmedYet = Error.Forbidden(
        "Community.NotConfirmedYet", "You can vouch for others once classmates have confirmed you.");

    /// <summary>Vouching twice for the same person.</summary>
    public static readonly Error AlreadyVouched = Error.Conflict(
        "Community.AlreadyVouched", "You've already said you know them.");

    /// <summary>Vouching for someone who's already confirmed.</summary>
    public static readonly Error AlreadyConfirmed = Error.Conflict(
        "Community.AlreadyConfirmed", "They're already confirmed. Thank you!");

    /// <summary>Adding a school: one with the same name and town is already on Msanga (however it's written).
    /// No "add anyway": two copies would split its classmates.</summary>
    public static readonly Error AlreadyListed = Error.Conflict(
        "Community.AlreadyListed", "That school is already on Msanga.");

    /// <summary>Adding a school: some look like it. The member can say theirs is different (addAnyway).</summary>
    public static readonly Error LooksLike = Error.Conflict(
        "Community.LooksLike", "Is it one of these? If not, you can still add yours.");
}
