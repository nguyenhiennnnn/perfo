using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class SupportRequest
{
    public int SupportRequestId { get; set; }
    public int OrderId { get; set; }
    public int? UserId { get; set; }
    public SupportRequestType Type { get; set; }
    public string Subject { get; set; } = null!;
    public SupportRequestStatus Status { get; set; } = SupportRequestStatus.New;
    public int? AssignedToUserId { get; set; }
    public int? ApologyPromotionId { get; set; }
    public int? SourcePaymentTransactionId { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public uint RowVersion { get; set; }

    public Order Order { get; set; } = null!;
    public User? User { get; set; }
    public User? AssignedToUser { get; set; }
    public Promotion? ApologyPromotion { get; set; }
    public PaymentTransaction? SourcePaymentTransaction { get; set; }
    public ICollection<SupportMessage> Messages { get; set; } = [];
}
