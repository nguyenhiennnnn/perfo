using System.Text.RegularExpressions;
using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Services.Promotions;
using Cinema.Application.Validators.Common;
using Cinema.Application.Validators.Pricing;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Promotions;

public partial class UpsertPromotionRequestValidator : AbstractValidator<UpsertPromotionRequest>
{
    public UpsertPromotionRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Must(code => CodeRegex().IsMatch(code.Trim()))
            .When(x => !string.IsNullOrWhiteSpace(x.Code))
            .WithMessage(Messages.Validation_Promotion_CodeFormat);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(FieldLengths.ComboName).NoControlCharacters();
        RuleFor(x => x.Description)
            .MaximumLength(FieldLengths.ComboDescription)
            .NoControlCharacters()
            .When(x => x.Description is not null);

        RuleFor(x => x.Type)
            .Must(v => PricingValidation.TryParseEnum<PromotionType>(v, out _))
            .WithMessage(Messages.Validation_Promotion_TypeInvalid);
        RuleFor(x => x.DiscountType)
            .Must(v => PricingValidation.TryParseEnum<PromotionDiscountType>(v, out _))
            .WithMessage(Messages.Validation_Promotion_DiscountTypeInvalid);
        RuleFor(x => x.Scope)
            .Must(v => PricingValidation.TryParseEnum<PromotionScope>(v, out _))
            .WithMessage(Messages.Validation_Promotion_ScopeInvalid);
        RuleFor(x => x.Channel)
            .Must(v => PricingValidation.TryParseEnum<SalesChannel>(v, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Channel))
            .WithMessage(Messages.Validation_Promotion_ChannelInvalid);

        RuleFor(x => x.MinOrderAmount)
            .Must(IsValidAmount)
            .When(x => x.MinOrderAmount is not null)
            .WithMessage(Messages.Validation_Promotion_MinOrderRange);
        RuleFor(x => x.BudgetAmount)
            .Must(v => IsValidAmount(v) && v > 0m)
            .When(x => x.BudgetAmount is not null)
            .WithMessage(Messages.Validation_Promotion_BudgetRange);

        RuleFor(x => x.TotalUsageLimit)
            .InclusiveBetween(1, PromotionLimits.MaxUsageLimit)
            .When(x => x.TotalUsageLimit is not null)
            .WithMessage(Messages.Validation_Promotion_LimitRange);
        RuleFor(x => x.PerCustomerLimit)
            .InclusiveBetween(1, PromotionLimits.MaxUsageLimit)
            .When(x => x.PerCustomerLimit is not null)
            .WithMessage(Messages.Validation_Promotion_LimitRange);

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, PromotionLimits.MaxPriority)
            .WithMessage(
                string.Format(
                    Messages.Validation_Setting_NumberRange,
                    "priority",
                    0,
                    PromotionLimits.MaxPriority
                )
            );

        RuleFor(x => x.EndsAt)
            .Must((request, endsAt) => endsAt > request.StartsAt)
            .When(x => x.StartsAt is not null && x.EndsAt is not null)
            .WithMessage(Messages.Validation_Promotion_ValidRangeOrder);

        RuleFor(x => x.MovieIds)
            .Must(ids => PricingValidation.IsValidIdList(ids))
            .WithMessage(Messages.Validation_IdList_Invalid);
        RuleFor(x => x.ShowtimeIds)
            .Must(ids => PricingValidation.IsValidIdList(ids))
            .WithMessage(Messages.Validation_IdList_Invalid);
        RuleFor(x => x.ScreenTypeIds)
            .Must(ids => PricingValidation.IsValidIdList(ids))
            .WithMessage(Messages.Validation_IdList_Invalid);
        RuleFor(x => x.MembershipTierIds)
            .Must(ids => PricingValidation.IsValidIdList(ids))
            .WithMessage(Messages.Validation_IdList_Invalid);
        RuleFor(x => x.DaysOfWeek)
            .Must(days => PricingValidation.IsValidDaysOfWeek(days))
            .WithMessage(Messages.Validation_DaysOfWeek_Invalid);

        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    ValidateDiscount(request, context);
                    ValidateAssignment(request, context);
                }
            );
    }

    private static void ValidateDiscount(
        UpsertPromotionRequest request,
        ValidationContext<UpsertPromotionRequest> context
    )
    {
        if (
            !PricingValidation.TryParseEnum<PromotionDiscountType>(
                request.DiscountType,
                out var type
            )
        )
            return;

        if (type == PromotionDiscountType.Percent)
        {
            if (
                request.DiscountValue is <= 0m or > 100m
                || decimal.Round(request.DiscountValue, 2) != request.DiscountValue
            )
                context.AddFailure("discountValue", Messages.Validation_Promotion_PercentRange);

            if (request.MaxDiscountAmount is { } cap && !(IsValidAmount(cap) && cap > 0m))
                context.AddFailure(
                    "maxDiscountAmount",
                    Messages.Validation_Promotion_MaxDiscountRange
                );
            return;
        }

        if (!(IsValidAmount(request.DiscountValue) && request.DiscountValue > 0m))
            context.AddFailure("discountValue", Messages.Validation_Promotion_AmountRange);

        if (request.MaxDiscountAmount is not null)
            context.AddFailure(
                "maxDiscountAmount",
                Messages.Validation_Promotion_MaxDiscountNotAllowed
            );
    }

    private static void ValidateAssignment(
        UpsertPromotionRequest request,
        ValidationContext<UpsertPromotionRequest> context
    )
    {
        if (!PricingValidation.TryParseEnum<PromotionType>(request.Type, out var type))
            return;

        if (type == PromotionType.PersonalCode)
        {
            if (request.AssignedUserId is null or <= 0)
                context.AddFailure(
                    "assignedUserId",
                    Messages.Validation_Promotion_AssignedUserRequired
                );
            return;
        }

        if (request.AssignedUserId is not null)
            context.AddFailure(
                "assignedUserId",
                Messages.Validation_Promotion_AssignedUserNotAllowed
            );
    }

    private static bool IsValidAmount(decimal? value) =>
        value is { } v
        && v >= 0m
        && v <= PromotionLimits.MaxAmount
        && v == decimal.Truncate(v);

    [GeneratedRegex(
        "^[A-Za-z0-9_-]{3,30}$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex CodeRegex();
}
