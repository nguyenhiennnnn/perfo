using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class CustomerVerification
{
    public int CustomerVerificationId { get; set; }
    public int UserId { get; set; }
    public RiskSignalType Reason { get; set; }
    public CustomerVerificationStatus Status { get; set; } = CustomerVerificationStatus.Pending;
    public string? CodeHash { get; set; }
    public DateTime? CodeSentAt { get; set; }
    public DateTime? CodeExpiresAt { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public User User { get; set; } = null!;
}
