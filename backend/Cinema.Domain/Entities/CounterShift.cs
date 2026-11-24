using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class CounterShift
{
    public int CounterShiftId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime OpenedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal? CountedCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? Variance { get; set; }
    public string? Note { get; set; }
    public DateTime? UnclosedAlertedAt { get; set; }
    public CounterShiftStatus Status { get; set; } = CounterShiftStatus.Open;
    public uint RowVersion { get; set; }

    public Employee Employee { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = [];
}
