using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.GiftCards;

public class PurchaseGiftCardRequestValidator : AbstractValidator<PurchaseGiftCardRequest>
{
    public PurchaseGiftCardRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage(Messages.Rule_GiftCardAmountInvalid);

        RuleFor(x => x.RecipientEmail)
            .NotEmpty()
            .WithMessage(Messages.Validation_Email_Required)
            .EmailAddress()
            .WithMessage(Messages.Validation_Email_Invalid)
            .MaximumLength(FieldLengths.Email)
            .WithMessage(Messages.Validation_Email_MaxLength150);

        RuleFor(x => x.RecipientName)
            .NotEmpty()
            .WithMessage(Messages.Rule_GiftCardRecipientRequired)
            .MaximumLength(FieldLengths.PersonName)
            .WithMessage(Messages.Validation_GiftCardRecipientName_MaxLength)
            .NoControlCharacters();

        RuleFor(x => x.Message!)
            .MaximumLength(FieldLengths.GiftCardMessage)
            .WithMessage(Messages.Validation_GiftCardMessage_MaxLength)
            .NoControlCharacters()
            .When(x => x.Message is not null);
    }
}
