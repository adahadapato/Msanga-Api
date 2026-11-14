namespace Msanga.Domain.Enums;

/// <summary>What kind of community it is. Only schools at launch; the column is there so others can come later
/// without changing the table.</summary>
public enum CommunityKind
{
    /// <summary>A school, college, university or polytechnic.</summary>
    School = 1
}

/// <summary>The level of a school (decided 13 Nov 2026: all three at launch).</summary>
public enum CommunityLevel
{
    /// <summary>Primary school.</summary>
    Primary = 1,

    /// <summary>Secondary school (FGC, Government College, private secondary schools).</summary>
    Secondary = 2,

    /// <summary>University, polytechnic or college of education ("University / Polytechnic" on screen).</summary>
    Tertiary = 3
}

/// <summary>Whether a community has been checked.</summary>
public enum CommunityStatus
{
    /// <summary>On Msanga's starting list, or checked by an admin.</summary>
    Listed = 1,

    /// <summary>Added by a member and not yet checked (admin review comes in M7). Shown with a "new" label.</summary>
    New = 2
}

/// <summary>Where a member stands in a school they joined (decided 14 Nov 2026: school first, then the year).</summary>
public enum CommunityMembershipStatus
{
    /// <summary>Says they were a student here; not vouched for yet (vouching comes in M3c).</summary>
    Joined = 1,

    /// <summary>Two confirmed members of the school said "I know them" (M3c).</summary>
    Confirmed = 2
}
