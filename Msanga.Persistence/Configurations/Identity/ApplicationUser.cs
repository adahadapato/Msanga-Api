using Microsoft.AspNetCore.Identity;

namespace Msanga.Persistence.Configurations.Identity;

/// <summary>
/// A member's sign-in account (table AspNetUsers): email, password hash, confirmed or not, lockout, and when they
/// last signed in. Managed by ASP.NET Core Identity's <c>UserManager</c>.
/// </summary>
/// <remarks>
/// <para>Everything about the PERSON (names, school surname, bio, photo) is in the Domain's <c>Member</c>, which has
/// the same Id. This class is only about signing in.</para>
/// <para>Ids are version-7 Guids (time-ordered), like every Domain Id.</para>
/// <para>No configuration class: <c>IdentityDbContext</c> maps Identity's tables itself.</para>
/// </remarks>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Creates a user with a new time-ordered Id.</summary>
    public ApplicationUser() => Id = Guid.CreateVersion7();

    /// <summary>When the user last signed in with their password (UTC); null if never.</summary>
    public DateTime? LastLoginAt { get; set; }
}
