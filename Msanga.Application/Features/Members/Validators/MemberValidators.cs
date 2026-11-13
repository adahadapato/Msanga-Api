using FluentValidation;
using Msanga.Application.Features.Members.Commands;
using Msanga.Domain.Entities;

namespace Msanga.Application.Features.Members.Validators;

/// <summary>Checks <see cref="UpdateMyProfileCommand"/> (the same limits the Member entity guards).</summary>
public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    /// <summary>The rules.</summary>
    public UpdateMyProfileCommandValidator()
    {
        var tooLong = $"Keep it to {Member.NameMaxLength} characters or fewer.";
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("Enter your first name.")
                                 .MaximumLength(Member.NameMaxLength).WithMessage(tooLong);
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Enter your last name.")
                                .MaximumLength(Member.NameMaxLength).WithMessage(tooLong);
        RuleFor(x => x.SchoolSurname).MaximumLength(Member.NameMaxLength).WithMessage(tooLong);
        RuleFor(x => x.Bio).MaximumLength(Member.BioMaxLength)
                           .WithMessage($"Keep it to {Member.BioMaxLength} characters or fewer.");
    }
}
