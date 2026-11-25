using Cinema.Application.DTOs.Waitlist;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Waitlist;

public class RegisterWaitlistRequestValidator : AbstractValidator<RegisterWaitlistRequest>
{
    public RegisterWaitlistRequestValidator()
    {
        RuleFor(x => x.SeatCount).GreaterThanOrEqualTo(1).WithMessage(Messages.Validation_Waitlist_SeatCountMin);
    }
}
