using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class WasteSheet
{
    public int WasteSheetId { get; set; }
    public DateOnly WasteDate { get; set; }
    public WasteReason Reason { get; set; }
    public string? Note { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<WasteSheetLine> Lines { get; set; } = [];
}
