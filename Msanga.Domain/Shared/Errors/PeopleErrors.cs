namespace Msanga.Domain.Shared.Errors;

/// <summary>Failures about finding people (M3d).</summary>
public static class PeopleErrors
{
    /// <summary>Searching before you're confirmed at any school (nobody's names are shown until then).</summary>
    public static readonly Error ConfirmFirst = Error.Forbidden(
        "People.ConfirmFirst", "Get confirmed at your school first: then you can find your schoolmates.");

    /// <summary>No such person, or not one you share a school with (the same answer, so nobody learns who's hidden).</summary>
    public static readonly Error NotFound = Error.NotFound(
        "People.NotFound", "We couldn't find that person.");
}
