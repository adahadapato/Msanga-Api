namespace Msanga.Domain.Constants;

/// <summary>
/// Identity role names. Seeded at start-up; use these constants instead of typing strings.
/// </summary>
public static class Roles
{
    /// <summary>Everyone with an account: given at registration.</summary>
    public const string Member = "Member";

    /// <summary>Runs Msanga (moderation, support). Nobody has it yet; given by hand in the database.</summary>
    public const string Admin = "Admin";

    /// <summary>All roles, for seeding.</summary>
    public static readonly IReadOnlyList<string> All = [Member, Admin];
}
