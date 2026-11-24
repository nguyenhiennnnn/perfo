using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class PaymentDailyStat
{
    public DateOnly BusinessDate { get; set; }
    public SalesChannel Channel { get; set; }
    public PaymentMethod Method { get; set; }
    public int TransactionCount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal GatewayFee { get; set; }
    public DateTime UpdatedAt { get; set; }
}
