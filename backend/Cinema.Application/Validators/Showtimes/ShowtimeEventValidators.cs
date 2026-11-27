using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Validators.Pricing;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Showtimes;

public class UpsertShowtimeEventRequestValidator : AbstractValidator<UpsertShowtimeEventRequest>
{
    public UpsertShowtimeEventRequestValidator()
    {
        RuleFor(x => x.AdjustmentMode)
            .Must(v => PricingValidation.TryParseEnum<PricingAdjustmentMode>(v, out _))
            .WithMessage(Messages.Validation_PricingRule_ModeInvalid);

        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    if (
                        !PricingValidation.TryParseEnum<PricingAdjustmentMode>(
                            request.AdjustmentMode,
                            out var mode
                        )
                        || PricingValidation.IsValidAdjustmentValue(mode, request.AdjustmentValue)
                    )
                        return;

                    context.AddFailure(
                        nameof(request.AdjustmentValue),
                        mode == PricingAdjustmentMode.Multiplier
                            ? Messages.Validation_PricingRule_MultiplierRange
                            : Messages.Validation_PricingRule_AmountRange
                    );
                }
            );
    }
}
