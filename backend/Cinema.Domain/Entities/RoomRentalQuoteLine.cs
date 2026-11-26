using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class RoomRentalQuoteLine
{
    public int RoomRentalQuoteLineId { get; set; }
    public int RoomRentalQuoteId { get; set; }
    public int SortOrder { get; set; }
    public RoomRentalQuoteLineKind Kind { get; set; }
    public string Label { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public int? ReferenceId { get; set; }

    public RoomRentalQuote? Quote { get; set; }
}
