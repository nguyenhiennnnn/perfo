using Cinema.Application.DTOs.Campaigns;
using Cinema.Application.Services.Promotions;
using Cinema.Application.Utils;
using Cinema.Application.Validators.Common;
using Cinema.Application.Validators.Pricing;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Campaigns;

public class UpsertCampaignRequestValidator : AbstractValidator<UpsertCampaignRequest>
{
    private const int MaxPromoValidityDays = 365;

    public UpsertCampaignRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(FieldLengths.CampaignName)
            .NoControlCharacters();
        RuleFor(x => x.SegmentId).GreaterThan(0).WithMessage(Messages.Validation_Campaign_SegmentRequired);
        RuleFor(x => x.Channel)
            .Must(v => PricingValidation.TryParseEnum<CampaignChannel>(v, out _))
            .WithMessage(Messages.Validation_Campaign_ChannelInvalid);
        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(FieldLengths.CampaignSubject)
            .NoControlCharacters()
            .Must(v => v is null || (!v.Contains('\n') && !v.Contains('\r')))
            .WithMessage(Messages.Validation_Campaign_SubjectSingleLine);
        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(FieldLengths.CampaignBody)
            .NoControlCharacters();
        RuleFor(x => x.ScheduledAt)
            .Must(v => BusinessTime.ToUtc(v!.Value) > DateTime.UtcNow)
            .When(x => x.ScheduledAt is not null)
            .WithMessage(Messages.Validation_Campaign_ScheduledAtPast);

        When(
            x => x.Promo is not null,
            () =>
                RuleFor(x => x.Promo!)
                    .Custom(
                        (promo, context) =>
                        {
                            if (
                                !PricingValidation.TryParseEnum<PromotionDiscountType>(
                                    promo.DiscountType,
                                    out var type
                                )
                            )
                            {
                                context.AddFailure(
                                    "promo.discountType",
                                    Messages.Validation_Promotion_DiscountTypeInvalid
                                );
                                return;
                            }

                            if (type == PromotionDiscountType.Percent)
                            {
                                if (
                                    promo.DiscountValue is <= 0m or > 100m
                                    || decimal.Round(promo.DiscountValue, 2) != promo.DiscountValue
                                )
                                    context.AddFailure(
                                        "promo.discountValue",
                                        Messages.Validation_Promotion_PercentRange
                                    );
                                if (
                                    promo.MaxDiscountAmount is { } cap
                                    && !(IsValidAmount(cap) && cap > 0m)
                                )
                                    context.AddFailure(
                                        "promo.maxDiscountAmount",
                                        Messages.Validation_Promotion_MaxDiscountRange
                                    );
                            }
                            else
                            {
                                if (!(IsValidAmount(promo.DiscountValue) && promo.DiscountValue > 0m))
                                    context.AddFailure(
                                        "promo.discountValue",
                                        Messages.Validation_Promotion_AmountRange
                                    );
                                if (promo.MaxDiscountAmount is not null)
                                    context.AddFailure(
                                        "promo.maxDiscountAmount",
                                        Messages.Validation_Promotion_MaxDiscountNotAllowed
                                    );
                            }

                            if (promo.ValidityDays is < 1 or > MaxPromoValidityDays)
                                context.AddFailure(
                                    "promo.validityDays",
                                    Messages.Validation_Campaign_PromoValidityRange
                                );
                        }
                    )
        );
    }

    private static bool IsValidAmount(decimal value) =>
        value >= 0m && value <= PromotionLimits.MaxAmount && value == decimal.Truncate(value);
}
