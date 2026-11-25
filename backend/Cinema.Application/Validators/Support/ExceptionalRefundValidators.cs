using Cinema.Application.DTOs.Support;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using FluentValidation;

namespace Cinema.Application.Validators.Support;

public class CreateExceptionalRefundProposalRequestValidator
    : AbstractValidator<CreateExceptionalRefundProposalRequest>
{
    public CreateExceptionalRefundProposalRequestValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.OrderLineId).GreaterThan(0).When(x => x.OrderLineId is not null);
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .Must(amount => amount == decimal.Round(amount, 0, MidpointRounding.AwayFromZero));
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(FieldLengths.RefundProposalReason)
            .NoControlCharacters();
    }
}

public class ApproveExceptionalRefundProposalRequestValidator
    : AbstractValidator<ApproveExceptionalRefundProposalRequest>
{
    public ApproveExceptionalRefundProposalRequestValidator()
    {
        RuleFor(x => x.Note!)
            .MaximumLength(FieldLengths.RefundProposalReason)
            .NoControlCharacters()
            .When(x => !string.IsNullOrEmpty(x.Note));
        RuleFor(x => x.CancelTicketLineIds!)
            .Must(ids => ids.Count <= 50 && ids.All(id => id > 0))
            .When(x => x.CancelTicketLineIds is not null);
    }
}

public class RejectExceptionalRefundProposalRequestValidator
    : AbstractValidator<RejectExceptionalRefundProposalRequest>
{
    public RejectExceptionalRefundProposalRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(FieldLengths.RefundProposalReason)
            .NoControlCharacters();
    }
}
