using Cinema.Application.DTOs.Loyalty;
using Cinema.Application.Services.Loyalty;
using Cinema.Application.Services.Promotions;
using Cinema.Application.Validators.Common;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Loyalty;

public class UpsertLoyaltyRewardRequestValidator : AbstractValidator<UpsertLoyaltyRewardRequest>
{
    public UpsertLoyaltyRewardRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(FieldLengths.ComboName).NoControlCharacters();

        RuleFor(x => x.Type)
            .Must(t => TryParse<LoyaltyRewardType>(t, out _))
            .WithMessage(Messages.Validation_LoyaltyReward_TypeInvalid);

        RuleFor(x => x.PointsCost)
            .InclusiveBetween(1, LoyaltyRewardLimits.MaxPointsCost)
            .WithMessage(Messages.Validation_LoyaltyReward_PointsCostRange);

        RuleFor(x => x.RemainingQuantity)
            .InclusiveBetween(0, LoyaltyRewardLimits.MaxQuantity)
            .WithMessage(Messages.Validation_LoyaltyReward_QuantityRange);

        RuleFor(x => x.CodeValidityDays)
            .InclusiveBetween(1, PromotionLimits.MaxPersonalCodeValidDays)
            .WithMessage(Messages.Validation_LoyaltyReward_CodeValidityRange);

        RuleFor(x => x.ValidTo)
            .Must((request, validTo) => validTo > request.ValidFrom)
            .When(x => x.ValidFrom is not null && x.ValidTo is not null)
            .WithMessage(Messages.Validation_LoyaltyReward_ValidRangeOrder);

        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    if (!TryParse<LoyaltyRewardType>(request.Type, out var type))
                        return;

                    if (type == LoyaltyRewardType.FreeCombo)
                    {
                        if (request.ComboId is not > 0)
                            context.AddFailure(
                                "comboId",
                                Messages.Validation_LoyaltyReward_ComboRequired
                            );
                    }
                    else if (request.ComboId is not null)
                        context.AddFailure(
                            "comboId",
                            Messages.Validation_LoyaltyReward_ComboNotAllowed
                        );

                    if (type != LoyaltyRewardType.Discount)
                    {
                        if (request.DiscountType is not null || request.DiscountValue is not null)
                            context.AddFailure(
                                "discountType",
                                Messages.Validation_LoyaltyReward_DiscountNotAllowed
                            );

                        if (type == LoyaltyRewardType.FreeTicket)
                        {
                            if (request.MaxDiscountAmount is not { } ticketCap || !IsValidAmount(ticketCap))
                                context.AddFailure(
                                    "maxDiscountAmount",
                                    Messages.Validation_LoyaltyReward_FreeTicketCapRequired
                                );
                        }
                        else if (request.MaxDiscountAmount is not null)
                            context.AddFailure(
                                "maxDiscountAmount",
                                Messages.Validation_LoyaltyReward_MaxDiscountNotAllowed
                            );
                        return;
                    }

                    if (
                        !TryParse<PromotionDiscountType>(request.DiscountType, out var discountType)
                    )
                    {
                        context.AddFailure(
                            "discountType",
                            Messages.Validation_LoyaltyReward_DiscountTypeInvalid
                        );
                        return;
                    }

                    var value = request.DiscountValue ?? 0m;
                    if (discountType == PromotionDiscountType.Percent)
                    {
                        if (value is <= 0m or > 100m || decimal.Round(value, 2) != value)
                            context.AddFailure(
                                "discountValue",
                                Messages.Validation_LoyaltyReward_DiscountPercentRange
                            );
                        if (request.MaxDiscountAmount is { } cap && !IsValidAmount(cap))
                            context.AddFailure(
                                "maxDiscountAmount",
                                Messages.Validation_LoyaltyReward_MaxDiscountRange
                            );
                        return;
                    }

                    if (!IsValidAmount(value))
                        context.AddFailure(
                            "discountValue",
                            Messages.Validation_LoyaltyReward_DiscountAmountRange
                        );
                    if (request.MaxDiscountAmount is not null)
                        context.AddFailure(
                            "maxDiscountAmount",
                            Messages.Validation_LoyaltyReward_MaxDiscountNotAllowed
                        );
                }
            );
    }

    private static bool TryParse<T>(string? value, out T result)
        where T : struct, Enum =>
        Enum.TryParse(value?.Trim(), true, out result)
        && Enum.IsDefined(result)
        && !int.TryParse(value!.Trim(), out _);

    private static bool IsValidAmount(decimal value) =>
        value > 0m && value <= PromotionLimits.MaxAmount && decimal.Round(value) == value;
}
