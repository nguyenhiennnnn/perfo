using Cinema.Application.DTOs.Bundles;
using Cinema.Application.Services.Bundles;
using Cinema.Application.Validators.Common;
using Cinema.Application.Validators.Pricing;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Bundles;

public class UpsertBundleRequestValidator : AbstractValidator<UpsertBundleRequest>
{
    public UpsertBundleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(FieldLengths.ComboName).NoControlCharacters();
        RuleFor(x => x.Description)
            .MaximumLength(FieldLengths.ComboDescription)
            .NoControlCharacters()
            .When(x => x.Description is not null);

        RuleFor(x => x.Price)
            .Must(v => v is > 0m and <= BundleLimits.MaxPrice && v == decimal.Truncate(v))
            .WithMessage(Messages.Validation_Bundle_PriceRange);

        RuleFor(x => x.Items)
            .Must(items => items is { Count: > 0 and <= BundleLimits.MaxItems })
            .WithMessage(string.Format(Messages.Validation_Bundle_ItemsCount, BundleLimits.MaxItems));
        RuleForEach(x => x.Items)
            .Must(item =>
                (item.TicketTypeId is null) != (item.ComboId is null)
                && (item.TicketTypeId ?? item.ComboId) > 0
                && item.Quantity is >= 1 and <= BundleLimits.MaxItemQuantity
            )
            .WithMessage(
                string.Format(Messages.Validation_Bundle_ItemInvalid, BundleLimits.MaxItemQuantity)
            );
        RuleFor(x => x.Items)
            .Must(items =>
                items!
                    .GroupBy(i => (i.TicketTypeId, i.ComboId))
                    .All(g => g.Count() == 1)
            )
            .When(x => x.Items is { Count: > 0 })
            .WithMessage(Messages.Validation_Bundle_ItemDuplicate);
        RuleFor(x => x.Items)
            .Must(items => items!.Any(i => i.TicketTypeId is not null))
            .When(x => x.Items is { Count: > 0 })
            .WithMessage(Messages.Validation_Bundle_TicketRequired);

        RuleFor(x => x.ValidTo)
            .Must((request, validTo) => validTo > request.ValidFrom)
            .When(x => x.ValidFrom is not null && x.ValidTo is not null)
            .WithMessage(Messages.Validation_Bundle_ValidRangeOrder);

        RuleFor(x => x.MovieIds)
            .Must(ids => PricingValidation.IsValidIdList(ids))
            .WithMessage(Messages.Validation_IdList_Invalid);
        RuleFor(x => x.ShowtimeIds)
            .Must(ids => PricingValidation.IsValidIdList(ids))
            .WithMessage(Messages.Validation_IdList_Invalid);
        RuleFor(x => x.SeatTypeIds)
            .Must(ids => PricingValidation.IsValidIdList(ids))
            .WithMessage(Messages.Validation_IdList_Invalid);
        RuleFor(x => x.DaysOfWeek)
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
                            Messages.Validation_Bundle_TimeWindowBoth
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
                        context.AddFailure("allowedToTime", Messages.Validation_TimeWindow_Order);
                }
            );

        RuleFor(x => x.DailyLimit)
            .InclusiveBetween(1, BundleLimits.MaxDailyLimit)
            .When(x => x.DailyLimit is not null)
            .WithMessage(
                string.Format(Messages.Validation_Bundle_DailyLimitRange, BundleLimits.MaxDailyLimit)
            );
    }
}
