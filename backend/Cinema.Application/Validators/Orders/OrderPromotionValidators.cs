using Cinema.Application.DTOs.Orders;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Orders;

public class ApplyPromotionRequestValidator : AbstractValidator<ApplyPromotionRequest>
{
    public ApplyPromotionRequestValidator()
    {
        RuleFor(x => x.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(Messages.Validation_PromotionCode_Required)
            .MaximumLength(FieldLengths.PromotionCode)
            .WithMessage(Messages.Validation_Promotion_CodeFormat);
    }
}
