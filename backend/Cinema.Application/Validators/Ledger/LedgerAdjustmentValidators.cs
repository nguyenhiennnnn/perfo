using Cinema.Application.DTOs.Ledger;
using Cinema.Application.Services.Ledger;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Ledger;

public class CreateLedgerAdjustmentRequestValidator : AbstractValidator<CreateLedgerAdjustmentRequest>
{
    public CreateLedgerAdjustmentRequestValidator()
    {
        RuleFor(x => x.OriginalDate).NotEmpty().WithMessage(Messages.Validation_LedgerAdjustment_DateRequired);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage(Messages.Validation_LedgerAdjustment_ReasonRequired)
            .MaximumLength(FieldLengths.LedgerAdjustmentReason)
            .WithMessage(Messages.Validation_LedgerAdjustment_ReasonMaxLength)
            .NoControlCharacters();

        RuleFor(x => x.TicketRevenueDelta).Must(IsValidAmount).WithMessage(Messages.Validation_LedgerAdjustment_AmountInvalid);
        RuleFor(x => x.ComboRevenueDelta).Must(IsValidAmount).WithMessage(Messages.Validation_LedgerAdjustment_AmountInvalid);
        RuleFor(x => x.GiftCardSoldDelta).Must(IsValidAmount).WithMessage(Messages.Validation_LedgerAdjustment_AmountInvalid);
        RuleFor(x => x.DiscountDelta).Must(IsValidAmount).WithMessage(Messages.Validation_LedgerAdjustment_AmountInvalid);
        RuleFor(x => x.RefundAmountDelta).Must(IsValidAmount).WithMessage(Messages.Validation_LedgerAdjustment_AmountInvalid);
        RuleFor(x => x.CancelledOrdersDelta)
            .InclusiveBetween(-LedgerAdjustmentLimits.MaxCount, LedgerAdjustmentLimits.MaxCount)
            .WithMessage(Messages.Validation_LedgerAdjustment_CountInvalid);

        RuleFor(x => x)
            .Must(x =>
                x.TicketRevenueDelta != 0m
                || x.ComboRevenueDelta != 0m
                || x.GiftCardSoldDelta != 0m
                || x.DiscountDelta != 0m
                || x.RefundAmountDelta != 0m
                || x.CancelledOrdersDelta != 0
            )
            .WithMessage(Messages.Validation_LedgerAdjustment_NothingToAdjust);
    }

    private static bool IsValidAmount(decimal value) =>
        Math.Abs(value) <= LedgerAdjustmentLimits.MaxAmount && value == decimal.Round(value);
}
