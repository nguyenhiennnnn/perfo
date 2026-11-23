using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Services.Pricing;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Pricing;

public class UpsertTicketTypeRequestValidator : AbstractValidator<UpsertTicketTypeRequest>
{
    public UpsertTicketTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(FieldLengths.Name).NoControlCharacters();

        RuleFor(x => x.PriceMultiplier)
            .Must(v =>
                v is >= PricingLimits.MinTicketTypeMultiplier
                    and <= PricingLimits.MaxTicketTypeMultiplier
                && decimal.Round(v, 4) == v
            )
            .WithMessage(Messages.Validation_TicketType_PriceMultiplierRange);

        RuleFor(x => x.AllowedAgeRatingIds)
            .Must(ids => PricingValidation.IsValidIdList(ids))
            .WithMessage(Messages.Validation_IdList_Invalid);

        RuleFor(x => x.AllowedDaysOfWeek)
            .Must(days => PricingValidation.IsValidDaysOfWeek(days))
            .WithMessage(Messages.Validation_DaysOfWeek_Invalid);

        RuleFor(x => x.AllowedFromTime)
            .Must(PricingValidation.IsValidTime)
            .When(x => !string.IsNullOrEmpty(x.AllowedFromTime))
            .WithMessage(Messages.Validation_TimeFormat_HourMinute);

        RuleFor(x => x.AllowedToTime)
            .Must(PricingValidation.IsValidTime)
            .When(x => !string.IsNullOrEmpty(x.AllowedToTime))
            .WithMessage(Messages.Validation_TimeFormat_HourMinute);

        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    var hasFrom = !string.IsNullOrEmpty(request.AllowedFromTime);
                    var hasTo = !string.IsNullOrEmpty(request.AllowedToTime);
                    if (hasFrom != hasTo)
                    {
                        context.AddFailure(
                            hasFrom ? "allowedToTime" : "allowedFromTime",
                            Messages.Validation_TicketType_TimeWindowBoth
                        );
                        return;
                    }

                    if (
                        hasFrom
                        && PricingValidation.IsValidTime(request.AllowedFromTime)
                        && PricingValidation.IsValidTime(request.AllowedToTime)
                        && PricingValidation.ParseTime(request.AllowedFromTime!)
                            >= PricingValidation.ParseTime(request.AllowedToTime!)
                    )
                    {
                        context.AddFailure(
                            "allowedToTime",
                            Messages.Validation_TimeWindow_Order
                        );
                    }
                }
            );
    }
}
