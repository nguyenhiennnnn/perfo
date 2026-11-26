using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.RoomRentals;

public class RoomRentalTrackingRequestValidator : AbstractValidator<RoomRentalTrackingRequest>
{
    public RoomRentalTrackingRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(FieldLengths.TokenHash);
    }
}

public class ChangeRoomRentalRequestStatusRequestValidator
    : AbstractValidator<ChangeRoomRentalRequestStatusRequest>
{
    public ChangeRoomRentalRequestStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Note!)
            .MaximumLength(FieldLengths.RoomRentalNote)
            .NoControlCharacters()
            .When(x => !string.IsNullOrEmpty(x.Note));
        RuleFor(x => x.Note)
            .NotEmpty()
            .When(x => x.Status == Cinema.Domain.Enums.RoomRentalRequestStatus.Cancelled)
            .WithMessage(Messages.Validation_RoomRental_CancelNoteRequired);
    }
}

public class CancelRoomRentalRequestValidator : AbstractValidator<CancelRoomRentalRequest>
{
    public CancelRoomRentalRequestValidator()
    {
        RuleFor(x => x.Note)
            .NotEmpty()
            .WithMessage(Messages.Validation_RoomRental_CancelNoteRequired)
            .MaximumLength(FieldLengths.RoomRentalNote)
            .NoControlCharacters();
    }
}

public class RoomRentalTrackingQuoteRequestValidator
    : AbstractValidator<RoomRentalTrackingQuoteRequest>
{
    public RoomRentalTrackingQuoteRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(FieldLengths.TokenHash);
    }
}

public class RoomRentalTrackingQuoteDeclineRequestValidator
    : AbstractValidator<RoomRentalTrackingQuoteDeclineRequest>
{
    public RoomRentalTrackingQuoteDeclineRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(FieldLengths.TokenHash);
        RuleFor(x => x.Reason!)
            .MaximumLength(FieldLengths.RoomRentalNote)
            .NoControlCharacters()
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
