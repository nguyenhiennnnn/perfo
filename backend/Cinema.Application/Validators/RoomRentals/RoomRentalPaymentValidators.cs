using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.RoomRentals;

public class CreateRoomRentalPaymentOrderRequestValidator
    : AbstractValidator<CreateRoomRentalPaymentOrderRequest>
{
    public CreateRoomRentalPaymentOrderRequestValidator()
    {
        RuleFor(x => x.Stage).IsInEnum();
    }
}

public class RecordRoomRentalBankTransferRequestValidator
    : AbstractValidator<RecordRoomRentalBankTransferRequest>
{
    public RecordRoomRentalBankTransferRequestValidator()
    {
        RuleFor(x => x.Stage).IsInEnum();
        RuleFor(x => x.Amount!.Value)
            .GreaterThan(0m)
            .WithMessage(Messages.Validation_RoomRental_AmountInvalid)
            .When(x => x.Amount.HasValue);
        RuleFor(x => x.Reference)
            .NotEmpty()
            .WithMessage(Messages.Validation_RoomRentalPayment_ReferenceRequired)
            .MaximumLength(FieldLengths.RoomRentalPaymentReference)
            .WithMessage(Messages.Validation_RoomRentalPayment_ReferenceMaxLength)
            .NoControlCharacters();
        RuleFor(x => x.Note!)
            .MaximumLength(FieldLengths.RoomRentalNote)
            .NoControlCharacters()
            .When(x => !string.IsNullOrEmpty(x.Note));
    }
}

public class RoomRentalTrackingCheckoutRequestValidator
    : AbstractValidator<RoomRentalTrackingCheckoutRequest>
{
    private static readonly string[] ValidMethods =
    [
        PaymentMethodNames.VnPay,
        PaymentMethodNames.Momo,
        PaymentMethodNames.Stripe,
    ];

    public RoomRentalTrackingCheckoutRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(FieldLengths.TokenHash);
        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .Must(m => ValidMethods.Contains(m))
            .WithMessage(Messages.Validation_PaymentMethod_Allowed);
    }
}

public class RoomRentalTrackingGiftCardRequestValidator
    : AbstractValidator<RoomRentalTrackingGiftCardRequest>
{
    public RoomRentalTrackingGiftCardRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(FieldLengths.TokenHash);
        RuleFor(x => x.Code).NotEmpty().WithMessage(Messages.Validation_GiftCardCode_Required);
    }
}
