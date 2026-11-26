namespace Cinema.Domain.Entities;

public class CustomerDevice
{
    public int UserId { get; set; }
    public string DeviceId { get; set; } = null!;
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }

    public User User { get; set; } = null!;
}
