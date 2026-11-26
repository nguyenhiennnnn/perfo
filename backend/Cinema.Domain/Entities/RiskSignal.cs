using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class RiskSignal
{
    public int RiskSignalId { get; set; }
    public int UserId { get; set; }
    public RiskSignalType Type { get; set; }
    public int? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
