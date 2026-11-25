using Cinema.Application.DTOs.Support;
using Cinema.Application.Services.Promotions;
using Cinema.Application.Validators.Common;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Support;

public class AssignSupportRequestRequestValidator : AbstractValidator<AssignSupportRequestRequest>
{
    public AssignSupportRequestRequestValidator()
    {
        RuleFor(x => x.AssigneeId).GreaterThan(0).When(x => x.AssigneeId is not null);
    }
}

public class ChangeSupportRequestStatusRequestValidator
    : AbstractValidator<ChangeSupportRequestStatusRequest>
{
    public ChangeSupportRequestStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => Enum.IsDefined(status) && status != SupportRequestStatus.New)
            .WithMessage(Messages.Validation_SupportRequest_StatusInvalid);
    }
}

public class StaffReplySupportRequestRequestValidator
    : AbstractValidator<StaffReplySupportRequestRequest>
{
    public StaffReplySupportRequestRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(FieldLengths.SupportMessageContent)
            .NoControlCharacters();
        RuleFor(x => x.Status)
            .Must(status => status is { } s && Enum.IsDefined(s) && s != SupportRequestStatus.New)
            .When(x => x.Status is not null)
            .WithMessage(Messages.Validation_SupportRequest_StatusInvalid);
    }
}

public class IssueSupportApologyCodeRequestValidator
    : AbstractValidator<IssueSupportApologyCodeRequest>
{
    public IssueSupportApologyCodeRequestValidator()
    {
        RuleFor(x => x.DiscountType).IsInEnum();

        RuleFor(x => x.DiscountValue)
            .Must(value => value is > 0m and <= 100m && decimal.Round(value, 2) == value)
            .When(x => x.DiscountType == PromotionDiscountType.Percent)
            .WithMessage(Messages.Validation_Promotion_PercentRange);
        RuleFor(x => x.DiscountValue)
            .Must(IsValidAmount)
            .When(x => x.DiscountType != PromotionDiscountType.Percent)
            .WithMessage(Messages.Validation_Promotion_AmountRange);

        RuleFor(x => x.MaxDiscountAmount)
            .Must(cap => cap is { } value && IsValidAmount(value))
            .When(x => x.MaxDiscountAmount is not null && x.DiscountType == PromotionDiscountType.Percent)
            .WithMessage(Messages.Validation_Promotion_MaxDiscountRange);
        RuleFor(x => x.MaxDiscountAmount)
            .Null()
            .When(x => x.DiscountType != PromotionDiscountType.Percent)
            .WithMessage(Messages.Validation_Promotion_MaxDiscountNotAllowed);

        RuleFor(x => x.ValidDays)
            .InclusiveBetween(1, PromotionLimits.MaxPersonalCodeValidDays)
            .When(x => x.ValidDays is not null)
            .WithMessage(
                string.Format(
                    Messages.Validation_Promotion_BulkValidDaysRange,
                    PromotionLimits.MaxPersonalCodeValidDays
                )
            );
    }

    private static bool IsValidAmount(decimal value) =>
        value > 0m && value <= PromotionLimits.MaxAmount && value == decimal.Truncate(value);
}
