namespace Msanga.Domain.Shared.Errors;

/// <summary>Failures about communities (schools).</summary>
public static class CommunityErrors
{
    /// <summary>No school with that Id (or it was removed).</summary>
    public static readonly Error NotFound = Error.NotFound(
        "Community.NotFound", "We couldn't find that school.");
}
