using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Services.RoomRentals;
using Cinema.Application.Utils;
using Cinema.Application.Validators.Common;
using Cinema.Application.Validators.Pricing;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.RoomRentals;

public class UpsertRoomRentalRateRequestValidator : AbstractValidator<UpsertRoomRentalRateRequest>
{
    public UpsertRoomRentalRateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(FieldLengths.ComboName).NoControlCharacters();

        RuleFor(x => x.RoomId)
            .GreaterThan(0)
            .WithMessage(Messages.Validation_RoomRentalRate_RoomIdPositive);

        RuleFor(x => x.StartTime)
            .Must(PricingValidation.IsValidTime)
            .WithMessage(Messages.Validation_RoomRentalRate_TimeInvalid);
        RuleFor(x => x.EndTime)
            .Must(PricingValidation.IsValidTime)
            .WithMessage(Messages.Validation_RoomRentalRate_TimeInvalid);
        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    if (
                        PricingValidation.IsValidTime(request.StartTime)
                        && PricingValidation.IsValidTime(request.EndTime)
                        && PricingValidation.ParseTime(request.StartTime)
                            >= PricingValidation.ParseTime(request.EndTime)
                    )
                    {
                        context.AddFailure("endTime", Messages.Validation_RoomRentalRate_TimeOrder);
                    }
                }
            );

        RuleFor(x => x.MinDurationMinutes)
            .InclusiveBetween(1, RoomRentalPricingLimits.MaxDurationMinutes)
            .WithMessage(Messages.Validation_RoomRentalRate_DurationRange);
        RuleFor(x => x.MaxDurationMinutes)
            .InclusiveBetween(1, RoomRentalPricingLimits.MaxDurationMinutes)
            .WithMessage(Messages.Validation_RoomRentalRate_DurationRange);
        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    if (request.MaxDurationMinutes < request.MinDurationMinutes)
                    {
                        context.AddFailure(
                            "maxDurationMinutes",
                            Messages.Validation_RoomRentalRate_DurationOrder
                        );
                    }
                }
            );

        RuleFor(x => x.BasePrice).Must(IsValidAmount).WithMessage(Messages.Validation_RoomRental_AmountInvalid);
        RuleFor(x => x.PerPersonFee)
            .Must(IsValidAmount)
            .WithMessage(Messages.Validation_RoomRental_AmountInvalid);
        RuleFor(x => x.IncludedAttendees)
            .InclusiveBetween(0, RoomRentalLimits.MaxAttendees)
            .WithMessage(Messages.Validation_RoomRentalRate_IncludedAttendeesRange);
    }

    private static bool IsValidAmount(decimal value) =>
        value >= 0m
        && value <= RoomRentalPricingLimits.MaxAmount
        && value == decimal.Truncate(value);
}

public class RoomRentalQuoteRequestValidator : AbstractValidator<RoomRentalQuoteRequest>
{
    public RoomRentalQuoteRequestValidator()
    {
        RuleFor(x => x.RoomId)
            .GreaterThan(0)
            .WithMessage(Messages.Validation_RoomRentalRate_RoomIdPositive);
        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(1, RoomRentalPricingLimits.MaxDurationMinutes)
            .WithMessage(Messages.Validation_RoomRentalRate_DurationRange);
        RuleFor(x => x.AttendeeCount)
            .InclusiveBetween(RoomRentalLimits.MinAttendees, RoomRentalLimits.MaxAttendees)
            .WithMessage(Messages.Validation_RoomRental_AttendeeCountRange);

        RuleFor(x => x.Combos)
            .Must(items =>
                items is null
                || (
                    items.Count <= RoomRentalPricingLimits.MaxComboItems
                    && items.Select(i => i.ComboId).Distinct().Count() == items.Count
                )
            )
            .WithMessage(Messages.Validation_RoomRentalQuote_ComboInvalid);
        RuleForEach(x => x.Combos)
            .Must(i =>
                i.ComboId > 0 && i.Quantity is >= 1 and <= RoomRentalPricingLimits.MaxComboQuantity
            )
            .WithMessage(Messages.Validation_RoomRentalQuote_ComboInvalid);

        RuleFor(x => x.Extras)
            .Must(items => items is null || items.Count <= RoomRentalPricingLimits.MaxExtraItems)
            .WithMessage(Messages.Validation_RoomRentalQuote_ExtraInvalid);
        RuleForEach(x => x.Extras)
            .Must(i =>
                !string.IsNullOrWhiteSpace(i.Label)
                && i.Label.Trim().Length <= RoomRentalPricingLimits.MaxLabelLength
                && !NameNormalizer.ContainsDisallowedCharacters(i.Label)
                && i.Amount > 0m
                && i.Amount <= RoomRentalPricingLimits.MaxAmount
                && i.Amount == decimal.Truncate(i.Amount)
                && i.Quantity is >= 1 and <= RoomRentalPricingLimits.MaxExtraQuantity
            )
            .WithMessage(Messages.Validation_RoomRentalQuote_ExtraInvalid);

        RuleFor(x => x.DiscountAmount)
            .Must(v =>
                v is null
                || (
                    v >= 0m
                    && v <= RoomRentalPricingLimits.MaxAmount
                    && v == decimal.Truncate(v.Value)
                )
            )
            .WithMessage(Messages.Validation_RoomRentalQuote_DiscountInvalid);
        RuleFor(x => x.DiscountPercent)
            .Must(v => v is null || (v >= 0m && v <= 100m && decimal.Round(v.Value, 2) == v))
            .WithMessage(Messages.Validation_RoomRentalQuote_DiscountInvalid);
        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    if (request.DiscountAmount is > 0m && request.DiscountPercent is > 0m)
                    {
                        context.AddFailure(
                            "discountPercent",
                            Messages.Validation_RoomRentalQuote_DiscountBoth
                        );
                    }
                }
            );
        RuleFor(x => x.DiscountReason)
            .MaximumLength(RoomRentalPricingLimits.MaxDiscountReasonLength)
            .When(x => x.DiscountReason is not null);
    }
}

public class UpsertRoomRentalQuoteVersionRequestValidator
    : AbstractValidator<UpsertRoomRentalQuoteVersionRequest>
{
    public UpsertRoomRentalQuoteVersionRequestValidator()
    {
        RuleFor(x => x.Pricing)
            .NotNull()
            .SetValidator(new RoomRentalQuoteRequestValidator());
        RuleFor(x => x.DepositPercent)
            .Must(v => v is null || (v > 0m && v <= 100m && decimal.Round(v.Value, 2) == v))
            .WithMessage(Messages.Validation_RoomRentalQuote_DepositPercentInvalid);
        RuleFor(x => x.CancellationTiers)
            .Must(RoomRentalCancellationPolicy.IsValid)
            .WithMessage(Messages.Validation_RoomRentalQuote_CancellationTiersInvalid);
        RuleFor(x => x.Note)
            .MaximumLength(FieldLengths.ComboDescription)
            .When(x => x.Note is not null);
    }
}
