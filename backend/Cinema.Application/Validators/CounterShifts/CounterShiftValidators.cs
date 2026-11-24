using Cinema.Application.DTOs.CounterShifts;
using Cinema.Application.Services.CounterShifts;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.CounterShifts;

public class OpenCounterShiftRequestValidator : AbstractValidator<OpenCounterShiftRequest>
{
    public OpenCounterShiftRequestValidator()
    {
        RuleFor(x => x.OpeningCash)
            .InclusiveBetween(0m, CounterShiftLimits.MaxCashAmount)
            .WithMessage(Messages.Validation_CounterShift_OpeningCashRange);
    }
}

public class AdminCloseCounterShiftRequestValidator
    : AbstractValidator<AdminCloseCounterShiftRequest>
{
    public AdminCloseCounterShiftRequestValidator()
    {
        RuleFor(x => x.CountedCash)
            .InclusiveBetween(0m, CounterShiftLimits.MaxCashAmount)
            .WithMessage(Messages.Validation_CounterShift_CountedCashRange);

        RuleFor(x => x.Note)
            .NotEmpty()
            .WithMessage(Messages.Validation_CounterShift_AdminCloseNoteRequired)
            .MaximumLength(FieldLengths.CounterShiftNote)
            .WithMessage(Messages.Validation_CounterShift_NoteMaxLength)
            .NoControlCharacters();
    }
}

public class CloseCounterShiftRequestValidator : AbstractValidator<CloseCounterShiftRequest>
{
    public CloseCounterShiftRequestValidator()
    {
        RuleFor(x => x.CountedCash)
            .InclusiveBetween(0m, CounterShiftLimits.MaxCashAmount)
            .WithMessage(Messages.Validation_CounterShift_CountedCashRange);

        RuleFor(x => x.Note!)
            .MaximumLength(FieldLengths.CounterShiftNote)
            .WithMessage(Messages.Validation_CounterShift_NoteMaxLength)
            .NoControlCharacters()
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
