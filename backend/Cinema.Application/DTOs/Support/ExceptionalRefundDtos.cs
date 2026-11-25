using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Support;

public record CreateExceptionalRefundProposalRequest(
    int OrderId,
    int? OrderLineId,
    decimal Amount,
    string Reason
);

public record ApproveExceptionalRefundProposalRequest(
    string? Note,
    IReadOnlyList<int>? CancelTicketLineIds = null
);

public record RejectExceptionalRefundProposalRequest(string Reason);

public record ExceptionalRefundProposalListFilter(
    ExceptionalRefundStatus? Status,
    int? OrderId,
    int? ProposedByUserId
);

public record ExceptionalRefundProposalDto(
    int ExceptionalRefundProposalId,
    int OrderId,
    string OrderCode,
    int? OrderLineId,
    string? OrderLineDescription,
    decimal Amount,
    string Reason,
    ExceptionalRefundStatus Status,
    bool RequiresAdminApproval,
    decimal ApprovalThreshold,
    int ProposedByUserId,
    string ProposedByName,
    DateTime ProposedAt,
    int? ReviewedByUserId,
    string? ReviewedByName,
    DateTime? ReviewedAt,
    string? ReviewNote
);
