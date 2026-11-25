using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Utils;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Orders;

public class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator(ICurrentActorAccessor actorAccessor)
    {
        RuleFor(x => x.Reason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(Messages.Validation_CancelReason_Required)
            .Must(value => OrderCancellationRules.TryParseReason(value, out _))
            .WithMessage(Messages.Invalid_CancelReasonNotAllowed)
            .Must(value =>
                OrderCancellationRules.TryParseReason(value, out var reason)
                && OrderCancellationRules.IsReasonAllowed(
                    OrderCancellationRules.KindForRole(actorAccessor.Current?.Role),
                    reason
                )
            )
            .WithMessage(Messages.Invalid_CancelReasonNotAllowed);
    }
}
