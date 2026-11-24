using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class ReconciliationItem
{
    public int ReconciliationItemId { get; set; }
    public int ReconciliationSessionId { get; set; }
    public ReconciliationGateway Gateway { get; set; }
    public string GatewayTransactionCode { get; set; } = null!;
    public decimal? Amount { get; set; }
    public decimal GatewayFee { get; set; }
    public string? GatewayStatus { get; set; }
    public DateTime? GatewayTime { get; set; }
    public int? PaymentTransactionId { get; set; }
    public ReconciliationResult Result { get; set; }
    public ReconciliationResolution Resolution { get; set; } = ReconciliationResolution.Open;
    public string? Note { get; set; }
    public int? AssignedToUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedByUserId { get; set; }

    public ReconciliationSession Session { get; set; } = null!;
    public PaymentTransaction? PaymentTransaction { get; set; }
    public User? AssignedToUser { get; set; }
    public User? ResolvedByUser { get; set; }
}
