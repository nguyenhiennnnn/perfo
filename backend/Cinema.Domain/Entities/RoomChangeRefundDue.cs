using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class RoomChangeRefundDue
{
    public int RoomChangeRefundDueId { get; set; }
    public int ShowtimeRoomChangeId { get; set; }
    public int OrderId { get; set; }
    public int OrderLineId { get; set; }
    public int FromSeatId { get; set; }
    public int ToSeatId { get; set; }
    public decimal AmountDue { get; set; }
    public RoomChangeRefundStatus Status { get; set; } = RoomChangeRefundStatus.Pending;
    public DateTime CreatedAt { get; set; }

    public ShowtimeRoomChange RoomChange { get; set; } = null!;
    public Order Order { get; set; } = null!;
    public OrderLine OrderLine { get; set; } = null!;
    public Seat FromSeat { get; set; } = null!;
    public Seat ToSeat { get; set; } = null!;
}
