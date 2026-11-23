using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Services.Pricing;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Pricing;

public class UpsertSpecialDayRequestValidator : AbstractValidator<UpsertSpecialDayRequest>
{
    private static readonly DateOnly MinDate = new(2000, 1, 1);
    private static readonly DateOnly MaxDate = new(2100, 12, 31);

    public UpsertSpecialDayRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(FieldLengths.PersonName).NoControlCharacters();

        RuleFor(x => x.StartDate)
            .Must(d => d >= MinDate && d <= MaxDate)
            .WithMessage(Messages.Validation_SpecialDay_DateRange);

        RuleFor(x => x.EndDate)
            .Must(d => d >= MinDate && d <= MaxDate)
            .WithMessage(Messages.Validation_SpecialDay_DateRange);

        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    if (request.EndDate < request.StartDate)
                    {
                        context.AddFailure(
                            "endDate",
                            Messages.Validation_SpecialDay_RangeOrder
                        );
                        return;
                    }

                    if (
                        request.EndDate.DayNumber - request.StartDate.DayNumber + 1
                        > PricingLimits.MaxSpecialDaySpanDays
                    )
                    {
                        context.AddFailure(
                            "endDate",
                            Messages.Validation_SpecialDay_RangeTooLong
                        );
                    }
                }
            );
    }
}
