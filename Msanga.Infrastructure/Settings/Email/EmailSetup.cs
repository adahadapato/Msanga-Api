using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Msanga.Domain.Constants;
using Msanga.Domain.Interfaces;

namespace Msanga.Infrastructure.Settings.Email;

/// <summary>Registers email: the settings, and SMTP or the outbox folder depending on <c>Email:Enabled</c>.</summary>
internal static class EmailSetup
{
    /// <summary>Adds email.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailSettings>()
                .Bind(configuration.GetSection(EmailSettings.SectionName))
                .ValidateDataAnnotations()
                .Validate(s => !s.Enabled || !string.IsNullOrWhiteSpace(s.Host),
                          "Email:Host is required when Email:Enabled is true.")
                .Validate(s => s.Enabled || !string.IsNullOrWhiteSpace(s.OutboxFolder),
                          "Email:OutboxFolder is required when Email:Enabled is false.")
                .ValidateOnStart();

        if (configuration.GetValue<bool>(AppSettings.Email.Enabled))
            services.AddTransient<IEmailSender, SmtpEmailSender>();
        else
            services.AddTransient<IEmailSender, OutboxFolderEmailSender>();

        return services;
    }
}
