using Cinema.Application.DTOs.Orders;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Orders;

public class ComboSelectionRequestValidator : AbstractValidator<ComboSelectionRequest>
{
    public ComboSelectionRequestValidator()
    {
        RuleFor(x => x.ComboId).GreaterThan(0).WithMessage(Messages.Validation_ComboId_Positive);

        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage(Messages.Validation_Quantity_Positive);
    }
}

public class BundleSelectionRequestValidator : AbstractValidator<BundleSelectionRequest>
{
    public BundleSelectionRequestValidator()
    {
        RuleFor(x => x.BundleId).GreaterThan(0).WithMessage(Messages.Validation_BundleId_Positive);

        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage(Messages.Validation_Quantity_Positive);
    }
}

public class SeatTicketSelectionRequestValidator : AbstractValidator<SeatTicketSelectionRequest>
{
    public SeatTicketSelectionRequestValidator()
    {
        RuleFor(x => x.ShowtimeSeatId).GreaterThan(0).WithMessage(Messages.Validation_SeatId_Positive);

        RuleFor(x => x.TicketTypeId)
            .GreaterThan(0)
            .When(x => x.TicketTypeId is not null)
            .WithMessage(Messages.Validation_TicketTypeId_Positive);

        RuleFor(x => x.ExpectedPrice)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.ExpectedPrice is not null)
            .WithMessage(Messages.Validation_ExpectedPrice_NonNegative);
    }
}

public static class SeatTicketSelectionRules
{
    public static IRuleBuilderOptions<T, List<SeatTicketSelectionRequest>?> ApplyTicketRules<T>(
        this IRuleBuilder<T, List<SeatTicketSelectionRequest>?> rule,
        Func<T, List<int>> seatIds
    ) =>
        rule.Must(
                (_, tickets) =>
                    tickets is null
                    || tickets.Select(t => t.ShowtimeSeatId).Distinct().Count() == tickets.Count
            )
            .WithMessage(Messages.Validation_Tickets_Duplicated)
            .Must(
                (root, tickets) =>
                    tickets is null
                    || tickets.All(t => seatIds(root).Contains(t.ShowtimeSeatId))
            )
            .WithMessage(Messages.Validation_Tickets_SeatNotSelected);
}

public class HoldSeatsRequestValidator : AbstractValidator<HoldSeatsRequest>
{
    public HoldSeatsRequestValidator()
    {
        RuleFor(x => x.ShowtimeId)
            .GreaterThan(0)
            .WithMessage(Messages.Validation_ShowtimeId_Positive);

        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .WithMessage(Messages.Validation_CustomerName_Required)
            .MaximumLength(FieldLengths.FullName)
            .WithMessage(Messages.Validation_CustomerName_MaxLength100);

        RuleFor(x => x.CustomerPhone)
            .Matches(PhoneRules.Pattern)
            .When(x => !string.IsNullOrEmpty(x.CustomerPhone))
            .WithMessage(Messages.Validation_Phone_Invalid)
            .MaximumLength(FieldLengths.Phone);

        RuleFor(x => x.SeatIds).NotEmpty().WithMessage(Messages.Validation_Seats_AtLeastOne);

        RuleForEach(x => x.SeatIds).GreaterThan(0).WithMessage(Messages.Validation_SeatId_Positive);

        RuleFor(x => x.SeatIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage(Messages.Validation_Seats_Duplicated);

        RuleForEach(x => x.Combos).SetValidator(new ComboSelectionRequestValidator());

        RuleForEach(x => x.Tickets).SetValidator(new SeatTicketSelectionRequestValidator());

        RuleFor(x => x.Tickets).ApplyTicketRules(x => x.SeatIds);

        RuleForEach(x => x.Bundles).SetValidator(new BundleSelectionRequestValidator());

        RuleFor(x => x.Bundles)
            .Must(b => b is null || b.Select(x => x.BundleId).Distinct().Count() == b.Count)
            .WithMessage(Messages.Validation_Bundles_Duplicated);
    }
}

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    private static readonly string[] ValidMethods =
    [
        PaymentMethodNames.VnPay,
        PaymentMethodNames.Momo,
        PaymentMethodNames.Stripe,
    ];

    public CheckoutRequestValidator()
    {
        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .Must(m => ValidMethods.Contains(m))
            .WithMessage(Messages.Validation_PaymentMethod_Allowed);
    }
}
