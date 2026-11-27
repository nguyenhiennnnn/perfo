using Cinema.Application.DTOs.Memberships;
using Cinema.Application.Utils;
using Cinema.Application.Validators.Common;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Memberships;

public class RefundMembershipSubscriptionRequestValidator
    : AbstractValidator<RefundMembershipSubscriptionRequest>
{
    public RefundMembershipSubscriptionRequestValidator()
    {
        RuleFor(x => x.Reason)
            .Must(v =>
                EnumConverters.TryParseEnum<MembershipRefundReason>(v, out var reason)
                && Enum.IsDefined(reason)
            )
            .WithMessage(Messages.Validation_MembershipRefund_ReasonInvalid);
        RuleFor(x => x.Note)
            .MaximumLength(FieldLengths.MembershipRefundNote)
            .NoControlCharacters()
            .When(x => x.Note is not null);
        RuleFor(x => x.Note)
            .NotEmpty()
            .WithMessage(Messages.Validation_MembershipRefund_NoteRequired)
            .When(x =>
                EnumConverters.TryParseEnum<MembershipRefundReason>(x.Reason, out var reason)
                && reason != MembershipRefundReason.CustomerRequest
            );
    }
}
