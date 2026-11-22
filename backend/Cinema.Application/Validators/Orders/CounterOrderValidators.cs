using Cinema.Application.DTOs.Orders;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Orders;

public class CounterHoldRequestValidator : AbstractValidator<CounterHoldRequest>
{
    public CounterHoldRequestValidator()
    {
        RuleFor(x => x.ShowtimeId)
            .GreaterThan(0)
            .WithMessage(Messages.Validation_ShowtimeId_Positive);

        RuleFor(x => x.SeatIds).NotEmpty().WithMessage(Messages.Validation_Seats_AtLeastOne);

        RuleForEach(x => x.SeatIds).GreaterThan(0).WithMessage(Messages.Validation_SeatId_Positive);

        RuleFor(x => x.SeatIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage(Messages.Validation_Seats_Duplicated);

        RuleForEach(x => x.Combos).SetValidator(new ComboSelectionRequestValidator());

        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .When(x => x.UserId is not null)
            .WithMessage(Messages.Validation_UserId_Positive);

        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .When(x => x.UserId is null)
            .WithMessage(Messages.Validation_UserIdOrCustomerNameRequired)
            .WithName("customerName");

        RuleFor(x => x.CustomerName)
            .MaximumLength(FieldLengths.FullName)
            .When(x => x.CustomerName is not null);

        RuleFor(x => x.CustomerPhone)
            .Matches(PhoneRules.Pattern)
            .When(x => !string.IsNullOrEmpty(x.CustomerPhone))
            .WithMessage(Messages.Validation_Phone_Invalid)
            .MaximumLength(FieldLengths.Phone);
    }
}

public class CounterComboOrderRequestValidator : AbstractValidator<CounterComboOrderRequest>
{
    public CounterComboOrderRequestValidator()
    {
        RuleFor(x => x.Combos).NotEmpty().WithMessage(Messages.Validation_Combos_AtLeastOne);

        RuleForEach(x => x.Combos).SetValidator(new ComboSelectionRequestValidator());

        RuleFor(x => x.Combos)
            .Must(combos => combos.Select(c => c.ComboId).Distinct().Count() == combos.Count)
            .WithMessage(Messages.Validation_Combos_Duplicated);

        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .When(x => x.UserId is not null)
            .WithMessage(Messages.Validation_UserId_Positive);

        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .When(x => x.UserId is null)
            .WithMessage(Messages.Validation_UserIdOrCustomerNameRequired)
            .WithName("customerName");

        RuleFor(x => x.CustomerName)
            .MaximumLength(FieldLengths.FullName)
            .When(x => x.CustomerName is not null);

        RuleFor(x => x.CustomerPhone)
            .Matches(PhoneRules.Pattern)
            .When(x => !string.IsNullOrEmpty(x.CustomerPhone))
            .WithMessage(Messages.Validation_Phone_Invalid)
            .MaximumLength(FieldLengths.Phone);
    }
}

public class CounterCheckoutRequestValidator : AbstractValidator<CounterCheckoutRequest>
{
    public CounterCheckoutRequestValidator()
    {
        RuleFor(x => x.PaymentMethod)
            .Must(CounterPaymentMethods.IsSupported)
            .WithMessage(Messages.Validation_CounterPaymentMethod_Invalid);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .When(x => x.Amount is not null)
            .WithMessage(Messages.Validation_Amount_Positive);

        RuleFor(x => x.AmountReceived)
            .NotNull()
            .When(x => CounterPaymentMethods.IsCash(x.PaymentMethod))
            .WithMessage(Messages.Validation_AmountReceived_RequiredForCash);

        RuleFor(x => x.AmountReceived)
            .GreaterThan(0)
            .When(x => x.AmountReceived is not null)
            .WithMessage(Messages.Validation_AmountReceived_Positive);

        RuleFor(x => x.AmountReceived)
            .Null()
            .When(x => CounterPaymentMethods.IsCard(x.PaymentMethod))
            .WithMessage(Messages.Validation_AmountReceived_CashOnly);
    }
}

public class CheckinRequestValidator : AbstractValidator<CheckinRequest>
{
    public CheckinRequestValidator()
    {
        RuleFor(x => x.QrCode).NotEmpty().WithMessage(Messages.Validation_QrCode_Required);
    }
}
