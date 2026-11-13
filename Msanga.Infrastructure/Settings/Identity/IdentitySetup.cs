using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Msanga.Domain.Interfaces;
using Msanga.Infrastructure.Services.Email;
using Msanga.Infrastructure.Services.Identity;
using Msanga.Persistence;
using Msanga.Persistence.Configurations.Identity;

namespace Msanga.Infrastructure.Settings.Identity;

/// <summary>
/// Registers accounts and sign-in: the settings, ASP.NET Core Identity (on the Msanga database), the token generator,
/// the account emails and the auth service.
/// </summary>
/// <remarks>
/// Confirm-email and reset-password codes are Msanga's own (<c>AccountCode</c>), not Identity's token providers, so no
/// token providers or data-protection keys are needed.
/// </remarks>
internal static class IdentitySetup
{
    /// <summary>Adds accounts and sign-in.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddMsangaIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Settings
        services.AddOptions<IdentitySettings>()
                .Bind(configuration.GetSection(IdentitySettings.SectionName))
                .ValidateDataAnnotations()
                .Validate(settings => IsValid(settings.Password) && IsValid(settings.Lockout) && IsValid(settings.Codes),
                          "Identity:Password, Identity:Lockout or Identity:Codes has a missing or out-of-range value.")
                .ValidateOnStart();

        services.AddOptions<JwtSettings>()
                .Bind(configuration.GetSection(JwtSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        // 2. Identity, on the same DbContext as everything else
        services.AddIdentityCore<ApplicationUser>()
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<MsangaDbContext>();

        // 3. Rules, from the settings (read when Identity first needs them)
        services.AddOptions<IdentityOptions>()
                .Configure<IOptions<IdentitySettings>>((options, settings) => Apply(options, settings.Value));

        // 4. Sign-in
        services.AddSingleton<JwtTokenGenerator>();            // stateless: settings + clock
        services.AddScoped<AccountEmails>();                   // the wording of the account emails
        services.AddScoped<IAuthService, AuthService>();       // per request: shares the request's database context

        return services;
    }

    /// <summary>Copies the settings into Identity's own options.</summary>
    private static void Apply(IdentityOptions options, IdentitySettings settings)
    {
        // Sign-in is by email (the user name IS the email), so it must be unique, and any character an email can
        // have is allowed (Identity's default list refuses e.g. an apostrophe)
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = string.Empty;
        options.SignIn.RequireConfirmedEmail = true;

        options.Password.RequiredLength = settings.Password.RequiredLength;
        options.Password.RequireDigit = settings.Password.RequireDigit;
        options.Password.RequireLowercase = settings.Password.RequireLowercase;
        options.Password.RequireUppercase = settings.Password.RequireUppercase;
        options.Password.RequireNonAlphanumeric = settings.Password.RequireNonAlphanumeric;
        options.Password.RequiredUniqueChars = settings.Password.RequiredUniqueChars;

        options.Lockout.MaxFailedAccessAttempts = settings.Lockout.MaxFailedAccessAttempts;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(settings.Lockout.LockoutMinutes);
        options.Lockout.AllowedForNewUsers = settings.Lockout.AllowedForNewUsers;
    }

    private static bool IsValid(object? settings) =>
        settings is not null &&
        Validator.TryValidateObject(settings, new ValidationContext(settings), null, validateAllProperties: true);
}
