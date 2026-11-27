using Cinema.Application.DTOs.Showtimes;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Showtimes;

public class EventGiftHandoverRequestValidator : AbstractValidator<EventGiftHandoverRequest>
{
    public EventGiftHandoverRequestValidator()
    {
        RuleFor(x => x.QrCode).NotEmpty().WithMessage(Messages.Validation_EventGiftHandover_QrRequired);
    }
}
