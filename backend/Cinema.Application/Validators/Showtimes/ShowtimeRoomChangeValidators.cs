using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Showtimes;

public class ShowtimeRoomChangeRequestValidator : AbstractValidator<ShowtimeRoomChangeRequest>
{
    private const int MaxOverrides = 2000;

    public ShowtimeRoomChangeRequestValidator()
    {
        RuleFor(x => x.TargetRoomId).GreaterThan(0).WithMessage(Messages.Invalid_RoomIdNotFound);

        RuleFor(x => x.Note)
            .MaximumLength(FieldLengths.ShowtimeRoomChangeNote)
            .WithMessage(Messages.Validation_RoomChange_NoteMaxLength)
            .NoControlCharacters()
            .When(x => !string.IsNullOrEmpty(x.Note));

        RuleFor(x => x.Overrides)
            .Must(overrides =>
                overrides is null
                || (
                    overrides.Count <= MaxOverrides
                    && overrides.All(o => o.ShowtimeSeatId > 0 && o.TargetSeatId > 0)
                    && overrides.Select(o => o.ShowtimeSeatId).Distinct().Count() == overrides.Count
                    && overrides.Select(o => o.TargetSeatId).Distinct().Count() == overrides.Count
                )
            )
            .WithMessage(Messages.Validation_RoomChange_InvalidMapping);
    }
}
