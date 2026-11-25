using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.GiftCards;

public class CancelGiftCardRequestValidator : AbstractValidator<CancelGiftCardRequest>
{
    public CancelGiftCardRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage(Messages.Validation_GiftCardCancelReason_Required)
            .MaximumLength(FieldLengths.GiftCardCancelReason)
            .WithMessage(Messages.Validation_GiftCardCancelReason_MaxLength)
            .NoControlCharacters();
        RuleFor(x => x.BalanceAction)
            .IsInEnum()
            .WithMessage(Messages.Validation_GiftCardCancelBalanceAction_Invalid)
            .When(x => x.BalanceAction is not null);
    }
}
