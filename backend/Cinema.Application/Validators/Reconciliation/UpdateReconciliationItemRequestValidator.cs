using Cinema.Application.DTOs.Reconciliation;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Reconciliation;

public class UpdateReconciliationItemRequestValidator : AbstractValidator<UpdateReconciliationItemRequest>
{
    public UpdateReconciliationItemRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Note is not null || x.AssignedToUserId is not null || x.ClearAssignee || x.Resolution is not null)
            .WithMessage(Messages.Validation_Reconciliation_UpdateEmpty);
        RuleFor(x => x.Note)
            .MaximumLength(FieldLengths.ErrorMessage)
            .WithMessage(Messages.Validation_Reconciliation_NoteTooLong)
            .NoControlCharacters()
            .When(x => x.Note is not null);
        RuleFor(x => x.AssignedToUserId)
            .GreaterThan(0)
            .WithMessage(Messages.Validation_Reconciliation_AssigneeInvalid)
            .When(x => x.AssignedToUserId is not null);
        RuleFor(x => x)
            .Must(x => !(x.ClearAssignee && x.AssignedToUserId is not null))
            .WithMessage(Messages.Validation_Reconciliation_AssigneeConflict);
        RuleFor(x => x.Resolution)
            .IsInEnum()
            .WithMessage(Messages.Validation_Reconciliation_ResolutionInvalid)
            .When(x => x.Resolution is not null);
    }
}
