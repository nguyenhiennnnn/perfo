using Cinema.Application.DTOs.Users;
using FluentValidation;

namespace Cinema.Application.Validators.Users;

public class UnsubscribeRequestValidator : AbstractValidator<UnsubscribeRequest>
{
    public UnsubscribeRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
