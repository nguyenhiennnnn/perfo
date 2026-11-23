using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Services.Pricing;
using Cinema.Application.Validators.Common;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Pricing;

public class UpsertPricingRuleRequestValidator : AbstractValidator<UpsertPricingRuleRequest>
{
    public UpsertPricingRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(FieldLengths.ComboName).NoControlCharacters();

        RuleFor(x => x.RuleType)
            .Must(v => PricingValidation.TryParseEnum<PricingRuleType>(v, out _))
            .WithMessage(Messages.Validation_PricingRule_TypeInvalid);

        RuleFor(x => x.AdjustmentMode)
            .Must(v => PricingValidation.TryParseEnum<PricingAdjustmentMode>(v, out _))
            .WithMessage(Messages.Validation_PricingRule_ModeInvalid);

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, PricingLimits.MaxPriority)
            .WithMessage(
                string.Format(
                    Messages.Validation_Setting_NumberRange,
                    "priority",
                    0,
                    PricingLimits.MaxPriority
                )
            );

        RuleFor(x => x.RoomIds)
            .Must(ids => PricingValidation.IsValidIdList(ids))
            .WithMessage(Messages.Validation_IdList_Invalid);
        RuleFor(x => x.ScreenTypeIds)
            .Must(ids => PricingValidation.IsValidIdList(ids))
            .WithMessage(Messages.Validation_IdList_Invalid);
        RuleFor(x => x.MovieIds)
            .Must(ids => PricingValidation.IsValidIdList(ids))
            .WithMessage(Messages.Validation_IdList_Invalid);

        RuleFor(x => x.ValidTo)
            .Must((request, validTo) => validTo >= request.ValidFrom)
            .When(x => x.ValidFrom is not null && x.ValidTo is not null)
            .WithMessage(Messages.Validation_PricingRule_ValidRangeOrder);

        RuleFor(x => x.Condition)
            .NotNull()
            .WithMessage(Messages.Validation_PricingRule_ConditionRequired);

        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    if (
                        request.Condition is null
                        || !PricingValidation.TryParseEnum<PricingRuleType>(
                            request.RuleType,
                            out var type
                        )
                        || !PricingValidation.TryParseEnum<PricingAdjustmentMode>(
                            request.AdjustmentMode,
                            out var mode
                        )
                    )
                        return;

                    switch (type)
                    {
                        case PricingRuleType.TimeOfDay:
                            ValidateTimeOfDay(request.Condition, context);
                            ValidateSingleValue(request.AdjustmentValue, mode, context);
                            break;
                        case PricingRuleType.WeekDay:
                            ValidateWeekDay(request.Condition, context);
                            ValidateSingleValue(request.AdjustmentValue, mode, context);
                            break;
                        case PricingRuleType.SpecialDay:
                            ValidateSpecialDay(request.Condition, context);
                            ValidateSingleValue(request.AdjustmentValue, mode, context);
                            break;
                        default:
                            ValidateOccupancy(request.Condition, mode, context);
                            break;
                    }
                }
            );
    }

    private static void ValidateSingleValue(
        decimal? value,
        PricingAdjustmentMode mode,
        ValidationContext<UpsertPricingRuleRequest> context
    )
    {
        if (value is null)
        {
            context.AddFailure(
                "adjustmentValue",
                Messages.Validation_PricingRule_AdjustmentRequired
            );
            return;
        }

        if (!PricingValidation.IsValidAdjustmentValue(mode, value.Value))
            context.AddFailure("adjustmentValue", ValueRangeMessage(mode));
    }

    private static string ValueRangeMessage(PricingAdjustmentMode mode) =>
        mode == PricingAdjustmentMode.Multiplier
            ? Messages.Validation_PricingRule_MultiplierRange
            : Messages.Validation_PricingRule_AmountRange;

    private static void ValidateTimeOfDay(
        PricingRuleConditionDto condition,
        ValidationContext<UpsertPricingRuleRequest> context
    )
    {
        if (string.IsNullOrEmpty(condition.StartTime) || string.IsNullOrEmpty(condition.EndTime))
        {
            context.AddFailure(
                "condition.startTime",
                Messages.Validation_PricingRule_TimeWindowRequired
            );
            return;
        }

        var startValid = PricingValidation.IsValidTime(condition.StartTime);
        var endValid = PricingValidation.IsValidTime(condition.EndTime);
        if (!startValid)
            context.AddFailure("condition.startTime", Messages.Validation_TimeFormat_HourMinute);
        if (!endValid)
            context.AddFailure("condition.endTime", Messages.Validation_TimeFormat_HourMinute);
        if (
            startValid
            && endValid
            && PricingValidation.ParseTime(condition.StartTime)
                >= PricingValidation.ParseTime(condition.EndTime)
        )
            context.AddFailure("condition.endTime", Messages.Validation_TimeWindow_Order);
    }

    private static void ValidateWeekDay(
        PricingRuleConditionDto condition,
        ValidationContext<UpsertPricingRuleRequest> context
    )
    {
        if (condition.DaysOfWeek is not { Count: > 0 })
        {
            context.AddFailure(
                "condition.daysOfWeek",
                Messages.Validation_PricingRule_WeekDayRequired
            );
            return;
        }

        if (!PricingValidation.IsValidDaysOfWeek(condition.DaysOfWeek))
            context.AddFailure("condition.daysOfWeek", Messages.Validation_DaysOfWeek_Invalid);
    }

    private static void ValidateSpecialDay(
        PricingRuleConditionDto condition,
        ValidationContext<UpsertPricingRuleRequest> context
    )
    {
        if (!PricingValidation.IsValidIdList(condition.SpecialDayIds))
            context.AddFailure("condition.specialDayIds", Messages.Validation_IdList_Invalid);
    }

    private static void ValidateOccupancy(
        PricingRuleConditionDto condition,
        PricingAdjustmentMode mode,
        ValidationContext<UpsertPricingRuleRequest> context
    )
    {
        var tiers = condition.Tiers;
        if (tiers is not { Count: > 0 })
        {
            context.AddFailure("condition.tiers", Messages.Validation_PricingRule_TiersRequired);
            return;
        }

        if (tiers.Count > PricingLimits.MaxOccupancyTiers)
        {
            context.AddFailure("condition.tiers", Messages.Validation_PricingRule_TooManyTiers);
            return;
        }

        var tiersValid = true;
        foreach (var tier in tiers)
        {
            var rangeValid =
                tier.FromPercent is >= 0m and <= 100m
                && (
                    tier.ToPercent is null
                    || (tier.ToPercent > tier.FromPercent && tier.ToPercent <= 100m)
                );
            if (!rangeValid)
            {
                tiersValid = false;
                context.AddFailure("condition.tiers", Messages.Validation_PricingRule_TierInvalid);
            }

            if (
                tier.MaxHoursBeforeStart is { } hours
                && (hours < 1 || hours > PricingLimits.MaxHoursBeforeStart)
            )
            {
                tiersValid = false;
                context.AddFailure(
                    "condition.tiers",
                    Messages.Validation_PricingRule_TierHoursRange
                );
            }

            if (!PricingValidation.IsValidAdjustmentValue(mode, tier.Value))
            {
                tiersValid = false;
                context.AddFailure("condition.tiers", ValueRangeMessage(mode));
            }
        }

        if (!tiersValid)
            return;

        var ordered = tiers.OrderBy(t => t.FromPercent).ToList();
        for (var i = 0; i < ordered.Count - 1; i++)
        {
            var upper = ordered[i].ToPercent;
            if (upper is null || upper > ordered[i + 1].FromPercent)
            {
                context.AddFailure("condition.tiers", Messages.Validation_PricingRule_TiersOverlap);
                return;
            }
        }
    }
}
