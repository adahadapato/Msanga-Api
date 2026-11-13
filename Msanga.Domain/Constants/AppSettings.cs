namespace Msanga.Domain.Constants;

/// <summary>
/// Every configuration key the API reads, in one place, so nothing types a key by hand.
/// </summary>
/// <remarks>
/// <para>Secrets (connection string, JWT key, SMTP password) are empty in <c>appsettings.json</c> and come from User
/// Secrets in development and from environment variables / the CI pipeline in production.</para>
/// <para>Settings classes are checked at start-up (<c>ValidateOnStart</c>), so a missing value stops the app with a
/// clear message.</para>
/// </remarks>
public static class AppSettings
{
    // ==================== Application ====================

    /// <summary>Addresses of the API and the web app (no trailing slash).</summary>
    public static class Application
    {
        public const string Section = "AppSettings";

        /// <summary>Where the API runs.</summary>
        public const string ApiBaseUrl = "AppSettings:ApiBaseUrl";

        /// <summary>Where the web app runs (for CORS-related checks and future use; emails carry codes, not links).</summary>
        public const string WebsiteBaseUrl = "AppSettings:WebsiteBaseUrl";
    }

    // ==================== OpenAPI / Swagger ====================

    /// <summary>The OpenAPI document and Swagger UI (switched off in production).</summary>
    public static class OpenApi
    {
        public const string Section = "OpenApi";

        public const string Enabled = "OpenApi:Enabled";

        public const string Title = "OpenApi:Title";

        public const string Version = "OpenApi:Version";

        public const string Description = "OpenApi:Description";

        public const string DocumentPath = "OpenApi:DocumentPath";

        public const string UiPath = "OpenApi:UiPath";

        public static class Contact
        {
            public const string Section = "OpenApi:Contact";

            public const string Name = "OpenApi:Contact:Name";

            public const string Email = "OpenApi:Contact:Email";

            public const string Url = "OpenApi:Contact:Url";
        }
    }

    // ==================== Connection strings ====================

    /// <summary>The SQL Server database.</summary>
    public static class ConnectionStrings
    {
        public const string Section = "ConnectionStrings";

        public const string DefaultName = "DefaultConnection";

        public const string Default = "ConnectionStrings:DefaultConnection";
    }

    // ==================== Identity ====================

    /// <summary>Password, lockout and emailed-code rules.</summary>
    public static class Identity
    {
        public const string Section = "Identity";

        public static class Password
        {
            public const string Section = "Identity:Password";

            public const string RequiredLength = "Identity:Password:RequiredLength";

            public const string RequireDigit = "Identity:Password:RequireDigit";

            public const string RequireLowercase = "Identity:Password:RequireLowercase";

            public const string RequireUppercase = "Identity:Password:RequireUppercase";

            public const string RequireNonAlphanumeric = "Identity:Password:RequireNonAlphanumeric";

            public const string RequiredUniqueChars = "Identity:Password:RequiredUniqueChars";
        }

        public static class Lockout
        {
            public const string Section = "Identity:Lockout";

            public const string MaxFailedAccessAttempts = "Identity:Lockout:MaxFailedAccessAttempts";

            public const string LockoutMinutes = "Identity:Lockout:LockoutMinutes";

            public const string AllowedForNewUsers = "Identity:Lockout:AllowedForNewUsers";
        }

        /// <summary>How long the emailed 6-digit codes work.</summary>
        public static class Codes
        {
            public const string Section = "Identity:Codes";

            public const string ConfirmEmailMinutes = "Identity:Codes:ConfirmEmailMinutes";

            public const string ResetPasswordMinutes = "Identity:Codes:ResetPasswordMinutes";
        }
    }

    // ==================== JWT ====================

    /// <summary>Access and refresh tokens.</summary>
    public static class Jwt
    {
        public const string Section = "Jwt";

        public const string Key = "Jwt:Key";

        public const string Issuer = "Jwt:Issuer";

        public const string Audience = "Jwt:Audience";

        public const string ExpiryMinutes = "Jwt:ExpiryMinutes";

        public const string RefreshTokenDays = "Jwt:RefreshTokenDays";
    }

    // ==================== CORS (the web app) ====================

    /// <summary>Which web apps may call the API from a browser.</summary>
    public static class Cors
    {
        public const string Section = "Cors";

        public const string AllowedOrigins = "Cors:AllowedOrigins";
    }

    // ==================== Email ====================

    /// <summary>The SMTP server emails are sent through (any provider: Brevo, Gmail, Zoho, your host's mail…).</summary>
    public static class Email
    {
        public const string Section = "Email";

        /// <summary>Send real emails (true), or write them to <see cref="OutboxFolder"/> (false, development).</summary>
        public const string Enabled = "Email:Enabled";

        public const string FromName = "Email:FromName";

        public const string FromAddress = "Email:FromAddress";

        /// <summary>Where replies to Msanga's emails go (a support inbox); empty = the from address.</summary>
        public const string ReplyToAddress = "Email:ReplyToAddress";

        public const string Host = "Email:Host";

        public const string Port = "Email:Port";

        public const string Security = "Email:Security";

        public const string Username = "Email:Username";

        public const string Password = "Email:Password";

        /// <summary>While email is off: the folder the emails are written to, relative to the app's folder.</summary>
        public const string OutboxFolder = "Email:OutboxFolder";
    }

    // ==================== Hosting ====================

    public static class Hosting
    {
        public const string AllowedHosts = "AllowedHosts";
    }

    // ==================== Logging ====================

    public static class Serilog
    {
        public const string Section = "Serilog";

        public const string MinimumLevelDefault = "Serilog:MinimumLevel:Default";
    }
}
