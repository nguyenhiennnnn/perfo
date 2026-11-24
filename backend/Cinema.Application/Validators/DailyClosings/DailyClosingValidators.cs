using Cinema.Application.DTOs.DailyClosings;
using Cinema.Application.Services.DailyClosings;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.DailyClosings;

public class CloseDailyClosingRequestValidator : AbstractValidator<CloseDailyClosingRequest>
{
    public CloseDailyClosingRequestValidator()
    {
        RuleFor(x => x.CountedCash)
            .InclusiveBetween(0m, DailyClosingLimits.MaxCashAmount)
            .WithMessage(Messages.Validation_DailyClosing_CountedCashRange);

        RuleFor(x => x.Note!)
            .MaximumLength(FieldLengths.DailyClosingNote)
            .WithMessage(Messages.Validation_DailyClosing_NoteMaxLength)
            .NoControlCharacters()
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
