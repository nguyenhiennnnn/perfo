using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class CustomerRestriction
{
    public int CustomerRestrictionId { get; set; }
    public int UserId { get; set; }
    public CustomerRestrictionReason Reason { get; set; }
    public CustomerRestrictionStatus Status { get; set; } = CustomerRestrictionStatus.Active;
    public int ExpiredOrderCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? LiftedAt { get; set; }
    public int? LiftedByUserId { get; set; }

    public User User { get; set; } = null!;
    public User? LiftedBy { get; set; }
}
