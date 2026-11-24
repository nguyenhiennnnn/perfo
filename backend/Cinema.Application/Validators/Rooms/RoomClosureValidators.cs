using Cinema.Application.DTOs.Rooms;
using Cinema.Application.Services.Rooms;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Rooms;

public class CreateRoomClosureRequestValidator : AbstractValidator<CreateRoomClosureRequest>
{
    public CreateRoomClosureRequestValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0).WithMessage(Messages.Invalid_RoomIdNotFound);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage(Messages.Validation_RoomClosure_ReasonRequired)
            .MaximumLength(FieldLengths.RoomClosureReason)
            .WithMessage(Messages.Validation_RoomClosure_ReasonMaxLength)
            .NoControlCharacters();

        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    if (request.EndTime <= request.StartTime)
                    {
                        context.AddFailure("endTime", Messages.Validation_RoomClosure_RangeOrder);
                        return;
                    }

                    if ((request.EndTime - request.StartTime).TotalDays > RoomClosureLimits.MaxSpanDays)
                        context.AddFailure(
                            "endTime",
                            string.Format(
                                Messages.Validation_RoomClosure_RangeTooLong,
                                RoomClosureLimits.MaxSpanDays
                            )
                        );
                }
            );
    }
}
