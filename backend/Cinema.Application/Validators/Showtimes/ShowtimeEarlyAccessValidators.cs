using Cinema.Application.DTOs.Showtimes;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Showtimes;

public class UpsertShowtimeEarlyAccessRequestValidator
    : AbstractValidator<UpsertShowtimeEarlyAccessRequest>
{
    public UpsertShowtimeEarlyAccessRequestValidator()
    {
        RuleFor(x => x.GeneralSaleStartTime)
            .Must(t => t != default)
            .WithMessage(Messages.Validation_EventEarlyAccess_TimeRequired);

        RuleFor(x => x.MembershipTierIds)
            .Must((request, tierIds) => request.PaidMembers || request.MovieInterest || tierIds is { Count: > 0 })
            .WithMessage(Messages.Validation_EventEarlyAccess_GroupRequired);

        RuleForEach(x => x.MembershipTierIds)
            .GreaterThan(0)
            .WithMessage(Messages.Validation_EventEarlyAccess_TierIdPositive);
    }
}
