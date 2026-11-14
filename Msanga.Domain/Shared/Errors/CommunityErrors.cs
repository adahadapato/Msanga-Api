namespace Msanga.Domain.Shared.Errors;

/// <summary>Failures about communities (schools).</summary>
public static class CommunityErrors
{
    /// <summary>No school with that Id (or it was removed).</summary>
    public static readonly Error NotFound = Error.NotFound(
        "Community.NotFound", "We couldn't find that school.");

    /// <summary>Adding a school: one with the same name and town is already on Msanga (however it's written).
    /// No "add anyway": two copies would split its classmates.</summary>
    public static readonly Error AlreadyListed = Error.Conflict(
        "Community.AlreadyListed", "That school is already on Msanga.");

    /// <summary>Adding a school: some look like it. The member can say theirs is different (addAnyway).</summary>
    public static readonly Error LooksLike = Error.Conflict(
        "Community.LooksLike", "Is it one of these? If not, you can still add yours.");
}
