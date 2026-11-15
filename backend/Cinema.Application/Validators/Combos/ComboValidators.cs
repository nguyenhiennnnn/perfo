using Cinema.Application.DTOs.Combos;
using Cinema.Shared.Constants;
using FluentValidation;

namespace Cinema.Application.Validators.Combos;

public class UpsertComboRequestValidator : AbstractValidator<UpsertComboRequest>
{
    public UpsertComboRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ImageUrl).MaximumLength(FieldLengths.Url);
    }
}
