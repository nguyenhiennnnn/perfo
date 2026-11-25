using Cinema.Application.DTOs.GiftCards;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.GiftCards;

public class ApplyGiftCardRequestValidator : AbstractValidator<ApplyGiftCardRequest>
{
    public ApplyGiftCardRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage(Messages.Validation_GiftCardCode_Required);
    }
}
