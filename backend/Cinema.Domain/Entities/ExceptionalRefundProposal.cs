using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class ExceptionalRefundProposal
{
    public int ExceptionalRefundProposalId { get; set; }
    public int OrderId { get; set; }
    public int? OrderLineId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = null!;
    public ExceptionalRefundStatus Status { get; set; } = ExceptionalRefundStatus.Pending;
    public bool RequiresAdminApproval { get; set; }
    public decimal ApprovalThreshold { get; set; }
    public int ProposedByUserId { get; set; }
    public DateTime ProposedAt { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public uint RowVersion { get; set; }

    public Order Order { get; set; } = null!;
    public OrderLine? OrderLine { get; set; }
    public User ProposedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
