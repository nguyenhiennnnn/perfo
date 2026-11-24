using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Services.Showtimes;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Showtimes;

public class BlockSeatsRequestValidator : AbstractValidator<BlockSeatsRequest>
{
    public BlockSeatsRequestValidator()
    {
        RuleFor(x => x.ShowtimeSeatIds)
            .NotEmpty()
            .WithMessage(Messages.Validation_Seats_AtLeastOne)
            .Must(ids => ids.Distinct().Count() <= SeatBlockLimits.MaxSeatsPerRequest)
            .WithMessage(
                string.Format(
                    Messages.Validation_SeatBlock_TooManySeats,
                    SeatBlockLimits.MaxSeatsPerRequest
                )
            );
        RuleForEach(x => x.ShowtimeSeatIds)
            .GreaterThan(0)
            .WithMessage(Messages.Validation_SeatId_Positive);

        RuleFor(x => x.Type)
            .Must(type => SeatBlockRules.TryParseManualType(type, out _))
            .WithMessage(Messages.Validation_SeatBlock_TypeInvalid);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage(Messages.Validation_SeatBlock_ReasonRequired)
            .MaximumLength(FieldLengths.SeatBlockReason)
            .WithMessage(Messages.Validation_SeatBlock_ReasonMaxLength)
            .NoControlCharacters();

        RuleFor(x => x.ExpiresAt)
            .Must(expiresAt => expiresAt is null || expiresAt > DateTime.UtcNow)
            .WithMessage(Messages.Validation_SeatBlock_ExpiresAtPast);
    }
}

public class UnblockSeatsRequestValidator : AbstractValidator<UnblockSeatsRequest>
{
    public UnblockSeatsRequestValidator()
    {
        RuleFor(x => x.ShowtimeSeatIds)
            .NotEmpty()
            .WithMessage(Messages.Validation_Seats_AtLeastOne)
            .Must(ids => ids.Distinct().Count() <= SeatBlockLimits.MaxSeatsPerRequest)
            .WithMessage(
                string.Format(
                    Messages.Validation_SeatBlock_TooManySeats,
                    SeatBlockLimits.MaxSeatsPerRequest
                )
            );
        RuleForEach(x => x.ShowtimeSeatIds)
            .GreaterThan(0)
            .WithMessage(Messages.Validation_SeatId_Positive);
    }
}
