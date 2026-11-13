using Microsoft.AspNetCore.Identity;

namespace Msanga.Persistence.Seeding;

/// <summary>Turns a failed <see cref="IdentityResult"/> into an exception with Identity's own reasons.</summary>
internal static class IdentityResultCheck
{
    /// <summary>Throws if Identity refused.</summary>
    /// <param name="result">What Identity answered.</param>
    /// <param name="what">What was being done, e.g. "create role 'Member'".</param>
    /// <exception cref="InvalidOperationException">Identity refused.</exception>
    public static void Ensure(IdentityResult result, string what)
    {
        if (result.Succeeded) return;
        throw new InvalidOperationException(
            $"Could not {what}: {string.Join(" ", result.Errors.Select(e => e.Description))}");
    }
}
