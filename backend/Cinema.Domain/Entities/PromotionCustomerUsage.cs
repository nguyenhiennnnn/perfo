namespace Cinema.Domain.Entities;

public class PromotionCustomerUsage
{
    public int PromotionId { get; set; }
    public int UserId { get; set; }
    public int ConsumedCount { get; set; }

    public Promotion Promotion { get; set; } = null!;
    public User User { get; set; } = null!;
}
