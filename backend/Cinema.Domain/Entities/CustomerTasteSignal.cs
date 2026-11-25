using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class CustomerTasteSignal
{
    public int UserId { get; set; }
    public TasteSignalKind Kind { get; set; }
    public int RefId { get; set; }
    public decimal Weight { get; set; }

    public CustomerTasteProfile Profile { get; set; } = null!;
}
