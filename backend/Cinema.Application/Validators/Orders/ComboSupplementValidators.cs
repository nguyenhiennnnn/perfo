using Cinema.Application.DTOs.Orders;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Orders;

public class ComboSupplementRequestValidator : AbstractValidator<ComboSupplementRequest>
{
    public ComboSupplementRequestValidator()
    {
        RuleFor(x => x.Combos)
            .NotEmpty()
            .WithMessage(Messages.Validation_Combos_AtLeastOne);
        RuleForEach(x => x.Combos).SetValidator(new ComboSelectionRequestValidator());
    }
}
