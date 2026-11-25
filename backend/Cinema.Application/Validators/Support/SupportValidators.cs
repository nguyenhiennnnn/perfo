using Cinema.Application.DTOs.Support;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using FluentValidation;

namespace Cinema.Application.Validators.Support;

public class CreateSupportRequestRequestValidator : AbstractValidator<CreateSupportRequestRequest>
{
    public CreateSupportRequestRequestValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(FieldLengths.SupportRequestSubject)
            .NoControlCharacters();
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(FieldLengths.SupportMessageContent)
            .NoControlCharacters();
    }
}

public class ReplySupportRequestRequestValidator : AbstractValidator<ReplySupportRequestRequest>
{
    public ReplySupportRequestRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(FieldLengths.SupportMessageContent)
            .NoControlCharacters();
    }
}
