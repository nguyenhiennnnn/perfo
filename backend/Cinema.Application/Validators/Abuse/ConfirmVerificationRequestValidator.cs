using Cinema.Application.DTOs.Abuse;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Abuse;

public class ConfirmVerificationRequestValidator : AbstractValidator<ConfirmVerificationRequest>
{
    public ConfirmVerificationRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage(Messages.Validation_VerificationCode_Required)
            .Matches(@"^\d{6}$")
            .WithMessage(Messages.Validation_VerificationCode_Format);
    }
}
