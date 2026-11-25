using Cinema.Application.DTOs.Loyalty;
using Cinema.Application.Services.Loyalty;
using Cinema.Application.Validators.Common;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Loyalty;

public class UpsertLoyaltyChallengeRequestValidator
    : AbstractValidator<UpsertLoyaltyChallengeRequest>
{
    public UpsertLoyaltyChallengeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(FieldLengths.ComboName).NoControlCharacters();

        RuleFor(x => x.Description)
            .MaximumLength(FieldLengths.ComboDescription)
            .NoControlCharacters()
            .When(x => x.Description is not null);

        RuleFor(x => x.ConditionType)
            .Must(v => TryParse<LoyaltyChallengeConditionType>(v, out _))
            .WithMessage(Messages.Validation_LoyaltyChallenge_ConditionInvalid);

        RuleFor(x => x.RewardType)
            .Must(v => TryParse<LoyaltyChallengeRewardType>(v, out _))
            .WithMessage(Messages.Validation_LoyaltyChallenge_RewardTypeInvalid);

        RuleFor(x => x.TargetCount)
            .InclusiveBetween(1, LoyaltyChallengeLimits.MaxTargetCount)
            .WithMessage(Messages.Validation_LoyaltyChallenge_TargetRange);

        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    if (request.EndsOn < request.StartsOn)
                        context.AddFailure("endsOn", Messages.Validation_LoyaltyChallenge_RangeOrder);
                    else if (
                        request.EndsOn.DayNumber - request.StartsOn.DayNumber + 1
                        > LoyaltyChallengeLimits.MaxSpanDays
                    )
                        context.AddFailure("endsOn", Messages.Validation_LoyaltyChallenge_RangeTooLong);

                    if (!TryParse<LoyaltyChallengeRewardType>(request.RewardType, out var type))
                        return;

                    if (type == LoyaltyChallengeRewardType.Points)
                    {
                        if (
                            request.RewardPoints
                            is not (> 0 and <= LoyaltyChallengeLimits.MaxRewardPoints)
                        )
                            context.AddFailure(
                                "rewardPoints",
                                Messages.Validation_LoyaltyChallenge_PointsRange
                            );
                        if (request.RewardGiftId is not null)
                            context.AddFailure(
                                "rewardGiftId",
                                Messages.Validation_LoyaltyChallenge_GiftNotAllowed
                            );
                        return;
                    }

                    if (request.RewardGiftId is not > 0)
                        context.AddFailure(
                            "rewardGiftId",
                            Messages.Validation_LoyaltyChallenge_GiftRequired
                        );
                    if (request.RewardPoints is not null)
                        context.AddFailure(
                            "rewardPoints",
                            Messages.Validation_LoyaltyChallenge_PointsNotAllowed
                        );
                }
            );
    }

    private static bool TryParse<T>(string? value, out T result)
        where T : struct, Enum =>
        Enum.TryParse(value?.Trim(), true, out result)
        && Enum.IsDefined(result)
        && !int.TryParse(value!.Trim(), out _);
}
