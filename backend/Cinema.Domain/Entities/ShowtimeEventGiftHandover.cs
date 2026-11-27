namespace Cinema.Domain.Entities;

public class ShowtimeEventGiftHandover
{
    public long ShowtimeEventGiftHandoverId { get; set; }
    public int ShowtimeEventGiftReservationId { get; set; }
    public int Quantity { get; set; }
    public int HandedByUserId { get; set; }
    public DateTime HandedAt { get; set; }

    public ShowtimeEventGiftReservation Reservation { get; set; } = null!;
    public User HandedByUser { get; set; } = null!;
}
