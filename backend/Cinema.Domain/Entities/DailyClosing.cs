using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class DailyClosing
{
    public int DailyClosingId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public DailyClosingStatus Status { get; set; } = DailyClosingStatus.Draft;
    public string? SnapshotJson { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? CountedCash { get; set; }
    public decimal? Variance { get; set; }
    public string? Note { get; set; }
    public int? ClosedByUserId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? ReportEmailedAt { get; set; }
    public uint RowVersion { get; set; }

    public User? ClosedByUser { get; set; }
}
