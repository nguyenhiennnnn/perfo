using Cinema.Application.DTOs.Segments;
using Cinema.Application.Services.Segments;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Segments;

public class UpsertCustomerSegmentRequestValidator : AbstractValidator<UpsertCustomerSegmentRequest>
{
    public UpsertCustomerSegmentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(FieldLengths.CustomerSegmentName)
            .NoControlCharacters();
        RuleFor(x => x.Description)
            .MaximumLength(FieldLengths.Description)
            .NoControlCharacters()
            .When(x => x.Description is not null);

        RuleFor(x => x.Conditions)
            .Custom(
                (conditions, context) =>
                {
                    var message = SegmentConditionCompiler.Validate(conditions) switch
                    {
                        SegmentConditionIssue.TooComplex =>
                            Messages.Validation_CustomerSegment_ConditionTooComplex,
                        SegmentConditionIssue.Invalid =>
                            Messages.Validation_CustomerSegment_ConditionInvalid,
                        _ => null,
                    };
                    if (message is not null)
                        context.AddFailure("conditions", message);
                }
            );
    }
}
