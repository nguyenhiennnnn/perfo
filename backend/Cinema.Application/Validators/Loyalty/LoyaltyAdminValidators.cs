using Cinema.Application.DTOs.Loyalty;
using Cinema.Application.Services.Loyalty;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Loyalty;

public class AdjustLoyaltyPointsRequestValidator : AbstractValidator<AdjustLoyaltyPointsRequest>
{
    public AdjustLoyaltyPointsRequestValidator()
    {
        RuleFor(x => x.Points)
            .NotEqual(0)
            .WithMessage(Messages.Validation_LoyaltyAdjustment_PointsRequired)
            .InclusiveBetween(-LoyaltyAdjustmentLimits.MaxPoints, LoyaltyAdjustmentLimits.MaxPoints)
            .WithMessage(Messages.Validation_LoyaltyAdjustment_PointsRange);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage(Messages.Validation_LoyaltyAdjustment_ReasonRequired)
            .MaximumLength(FieldLengths.LoyaltyAdjustmentReason)
            .WithMessage(Messages.Validation_LoyaltyAdjustment_ReasonMaxLength)
            .NoControlCharacters();
    }
}
