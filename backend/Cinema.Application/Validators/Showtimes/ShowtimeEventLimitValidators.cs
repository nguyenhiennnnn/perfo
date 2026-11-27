using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Services.Showtimes;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Showtimes;

public class UpsertShowtimeEventLimitsRequestValidator
    : AbstractValidator<UpsertShowtimeEventLimitsRequest>
{
    public UpsertShowtimeEventLimitsRequestValidator()
    {
        RuleFor(x => x.MaxTicketsPerCustomer)
            .InclusiveBetween(1, ShowtimeEventLimitRules.MaxLimit)
            .When(x => x.MaxTicketsPerCustomer.HasValue)
            .WithMessage(string.Format(Messages.Validation_EventLimit_Range, ShowtimeEventLimitRules.MaxLimit));

        RuleFor(x => x.TicketQuotas)
            .Must(q => q is null || q.Select(i => i.TicketTypeId).Distinct().Count() == q.Count)
            .WithMessage(Messages.Validation_EventLimit_TicketTypeDuplicate);

        RuleForEach(x => x.TicketQuotas)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.TicketTypeId)
                    .GreaterThan(0)
                    .WithMessage(Messages.Validation_TicketTypeId_Positive);
                item.RuleFor(i => i.MaxTickets)
                    .InclusiveBetween(1, ShowtimeEventLimitRules.MaxLimit)
                    .WithMessage(string.Format(Messages.Validation_EventLimit_Range, ShowtimeEventLimitRules.MaxLimit));
            });
    }
}
