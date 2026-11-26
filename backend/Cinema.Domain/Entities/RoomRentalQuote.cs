namespace Cinema.Domain.Entities;

public class RoomRentalQuote
{
    public int RoomRentalQuoteId { get; set; }
    public int RoomRentalRequestId { get; set; }
    public int VersionNumber { get; set; }
    public int RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public int RoomRentalRateId { get; set; }
    public string RoomRentalRateName { get; set; } = null!;
    public DateTime StartAt { get; set; }
    public int DurationMinutes { get; set; }
    public int AttendeeCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal Total { get; set; }
    public decimal DepositPercent { get; set; }
    public decimal DepositAmount { get; set; }
    public DateTime ValidUntil { get; set; }
    public DateTime BalanceDueAt { get; set; }
    public string CancellationPolicyJson { get; set; } = "[]";
    public string? Note { get; set; }
    public DateTime? SentAt { get; set; }
    public string? SentToEmail { get; set; }
    public DateTime CreatedAt { get; set; }

    public RoomRentalRequest? Request { get; set; }
    public ICollection<RoomRentalQuoteLine> Lines { get; set; } = [];
}
