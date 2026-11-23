using Cinema.Application.DTOs.Orders;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Orders;

public class ComboPickupRequestValidator : AbstractValidator<ComboPickupRequest>
{
    public ComboPickupRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage(Messages.Validation_ComboPickup_CodeRequired)
            .MaximumLength(FieldLengths.QrCode);

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.OrderLineId)
                    .GreaterThan(0)
                    .WithMessage(Messages.Validation_OrderLineId_Positive);
                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage(Messages.Validation_ComboPickup_QuantityPositive);
            })
            .When(x => x.Items is not null);

        RuleFor(x => x.Items)
            .Must(items => items!.Select(i => i.OrderLineId).Distinct().Count() == items!.Count)
            .When(x => x.Items is not null)
            .WithMessage(Messages.Validation_ComboPickup_ItemsDuplicated);
    }
}
