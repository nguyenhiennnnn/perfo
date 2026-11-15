using Cinema.Application.DTOs.Users;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Users;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    private static readonly string[] ValidGenders = Enum.GetNames<Gender>();

    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(FieldLengths.FullName);
        RuleFor(x => x.Phone)
            .Matches(PhoneRules.Pattern)
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage(Messages.Validation_Phone_Invalid)
            .MaximumLength(FieldLengths.Phone);
        RuleFor(x => x.Avatar).MaximumLength(FieldLengths.Url);
        RuleFor(x => x.Gender)
            .Must(g => ValidGenders.Contains(g))
            .When(x => x.Gender is not null)
            .WithMessage(
                string.Format(
                    Messages.Validation_Gender_AllowedValues,
                    string.Join(", ", ValidGenders)
                )
            );
    }
}
