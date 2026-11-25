using Cinema.Application.DTOs.Combos;
using Cinema.Shared.Constants;
using FluentValidation;

namespace Cinema.Application.Validators.Combos;

public class UpsertComboRequestValidator : AbstractValidator<UpsertComboRequest>
{
    public UpsertComboRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(FieldLengths.ComboName);
        RuleFor(x => x.Description).MaximumLength(FieldLengths.ComboDescription);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ImageUrl).MaximumLength(FieldLengths.Url);
        RuleFor(x => x.SuggestedTicketCount).InclusiveBetween(0, 20).When(x => x.SuggestedTicketCount.HasValue);
    }
}
