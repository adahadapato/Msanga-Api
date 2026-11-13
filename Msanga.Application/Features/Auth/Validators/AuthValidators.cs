using FluentValidation;
using Msanga.Application.Features.Auth.Commands;
using Msanga.Domain.Entities;

namespace Msanga.Application.Features.Auth.Validators;

/// <summary>Rules shared by the account forms, with messages written for the person filling them in.</summary>
internal static class AccountRules
{
    /// <summary>Longest email (Identity's column).</summary>
    public const int EmailMaxLength = 256;

    /// <summary>Longest password accepted (a passphrase can be long; this only stops abuse).</summary>
    public const int PasswordMaxLength = 256;

    /// <summary>Longest refresh token accepted.</summary>
    public const int TokenMaxLength = 500;

    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Enter your email address.")
            .MaximumLength(EmailMaxLength).WithMessage($"Keep it to {EmailMaxLength} characters or fewer.")
            .EmailAddress().WithMessage("Enter an email address like name@example.com.");

    public static IRuleBuilderOptions<T, string> Name<T>(this IRuleBuilder<T, string> rule, string whenBlank) =>
        rule.NotEmpty().WithMessage(whenBlank)
            .MaximumLength(Member.NameMaxLength).WithMessage($"Keep it to {Member.NameMaxLength} characters or fewer.");

    /// <summary>A new password: present and not absurdly long. Identity checks the strength rules (length etc.).</summary>
    public static IRuleBuilderOptions<T, string> NewPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Choose a password.")
            .MaximumLength(PasswordMaxLength).WithMessage($"Keep it to {PasswordMaxLength} characters or fewer.");

    /// <summary>A 6-digit code from the email; spaces and dashes are allowed ("482 913").</summary>
    public static IRuleBuilderOptions<T, string> Code<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Enter the 6-digit code from the email.")
            .Must(typed => CodeText.Digits(typed) is { Length: AccountCode.Digits } digits && digits.All(char.IsAsciiDigit))
            .WithMessage("The code is 6 digits, like 482 913.");
}

/// <summary>Checks <see cref="RegisterCommand"/>.</summary>
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    /// <summary>The rules.</summary>
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FirstName).Name("Enter your first name.");
        RuleFor(x => x.LastName).Name("Enter your last name.");
        RuleFor(x => x.SchoolSurname).MaximumLength(Member.NameMaxLength)
                                     .WithMessage($"Keep it to {Member.NameMaxLength} characters or fewer.");
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Password).NewPassword();
    }
}

/// <summary>Checks <see cref="ConfirmEmailCommand"/>.</summary>
public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    /// <summary>The rules.</summary>
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Code).Code();
    }
}

/// <summary>Checks <see cref="ResendConfirmationCommand"/>.</summary>
public sealed class ResendConfirmationCommandValidator : AbstractValidator<ResendConfirmationCommand>
{
    /// <summary>The rules.</summary>
    public ResendConfirmationCommandValidator() => RuleFor(x => x.Email).Email();
}

/// <summary>Checks <see cref="LoginCommand"/>.</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>The rules.</summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Password).NotEmpty().WithMessage("Enter your password.")
                                .MaximumLength(AccountRules.PasswordMaxLength);
    }
}

/// <summary>Checks <see cref="RefreshTokenCommand"/>.</summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>The rules.</summary>
    public RefreshTokenCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(AccountRules.TokenMaxLength);
}

/// <summary>Checks <see cref="LogoutCommand"/>.</summary>
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    /// <summary>The rules.</summary>
    public LogoutCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(AccountRules.TokenMaxLength);
}

/// <summary>Checks <see cref="ForgotPasswordCommand"/>.</summary>
public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    /// <summary>The rules.</summary>
    public ForgotPasswordCommandValidator() => RuleFor(x => x.Email).Email();
}

/// <summary>Checks <see cref="ResetPasswordCommand"/>.</summary>
public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    /// <summary>The rules.</summary>
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Code).Code();
        RuleFor(x => x.NewPassword).NewPassword();
    }
}

/// <summary>Checks <see cref="ChangePasswordCommand"/>.</summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    /// <summary>The rules.</summary>
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Enter your current password.");
        RuleFor(x => x.NewPassword).NewPassword()
                                   .NotEqual(x => x.CurrentPassword)
                                   .WithMessage("The new password must be different from the current one.");
    }
}
