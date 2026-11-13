namespace Msanga.Domain.Constants;

/// <summary>
/// Authorization policy names. Each policy is registered in Presentation; the comment says who passes it.
/// </summary>
public static class Policies
{
    /// <summary>Any signed-in member.</summary>
    public const string RequireMember = "RequireMember";

    /// <summary>Admins only.</summary>
    public const string RequireAdmin = "RequireAdmin";
}
