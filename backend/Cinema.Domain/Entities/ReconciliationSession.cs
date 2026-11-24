using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class ReconciliationSession
{
    public int ReconciliationSessionId { get; set; }
    public ReconciliationGateway Gateway { get; set; }
    public ReconciliationSource Source { get; set; }
    public string? FileName { get; set; }
    public string? FileHash { get; set; }
    public DateOnly? PeriodFrom { get; set; }
    public DateOnly? PeriodTo { get; set; }
    public ReconciliationStatus Status { get; set; } = ReconciliationStatus.Running;
    public int TotalRows { get; set; }
    public int MatchedCount { get; set; }
    public int MissingInSystemCount { get; set; }
    public int MissingAtGatewayCount { get; set; }
    public int AmountMismatchCount { get; set; }
    public int StatusMismatchCount { get; set; }
    public int PaidAtGatewayOrderUnconfirmedCount { get; set; }
    public decimal GatewayTotalAmount { get; set; }
    public decimal SystemTotalAmount { get; set; }
    public string? ErrorMessage { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public User CreatedByUser { get; set; } = null!;
    public ICollection<ReconciliationItem> Items { get; set; } = [];
}
