using Cinema.Application.DTOs.Memberships;
using Cinema.Application.Services.Memberships;
using Cinema.Application.Utils;
using Cinema.Application.Validators.Common;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Memberships;

public class UpsertMembershipPlanRequestValidator : AbstractValidator<UpsertMembershipPlanRequest>
{
    public UpsertMembershipPlanRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(FieldLengths.ComboName).NoControlCharacters();
        RuleFor(x => x.Description)
            .MaximumLength(FieldLengths.ComboDescription)
            .NoControlCharacters()
            .When(x => x.Description is not null);
        RuleFor(x => x.Period)
            .Must(v =>
                EnumConverters.TryParseEnum<MembershipPlanPeriod>(v, out var period)
                && Enum.IsDefined(period)
            )
            .WithMessage(Messages.Validation_MembershipPlan_PeriodInvalid);
        RuleFor(x => x.Price)
            .Must(v => v is > 0m and <= MembershipPlanRules.MaxPrice && v == decimal.Truncate(v))
            .WithMessage(
                string.Format(
                    Messages.Validation_MembershipPlan_PriceRange,
                    MembershipPlanRules.MaxPrice.ToString("N0")
                )
            );
        RuleFor(x => x.TicketDiscountPercent)
            .InclusiveBetween(0m, 100m)
            .WithMessage(Messages.Validation_MembershipPlan_PercentRange);
        RuleFor(x => x.ComboDiscountPercent)
            .InclusiveBetween(0m, 100m)
            .WithMessage(Messages.Validation_MembershipPlan_PercentRange);
        RuleFor(x => x.FreeTicketConversionValue)
            .Must(v => v is >= 0m and <= MembershipPlanRules.MaxPrice && v == decimal.Truncate(v))
            .WithMessage(
                string.Format(
                    Messages.Validation_MembershipPlan_ConversionRange,
                    MembershipPlanRules.MaxPrice.ToString("N0")
                )
            )
            .Must(
                (request, v) =>
                    !EnumConverters.TryParseEnum<MembershipPlanPeriod>(request.Period, out var period)
                    || !Enum.IsDefined(period)
                    || v * request.FreeTicketsPerMonth * MembershipPlanRules.DurationMonths(period)
                        <= request.Price
            )
            .WithMessage(Messages.Validation_MembershipPlan_ConversionExceedsPrice);
        RuleFor(x => x.FreeTicketsPerMonth)
            .InclusiveBetween(0, MembershipPlanRules.MaxFreeTicketsPerMonth)
            .WithMessage(
                string.Format(
                    Messages.Validation_MembershipPlan_FreeTicketsRange,
                    MembershipPlanRules.MaxFreeTicketsPerMonth
                )
            );
    }
}
