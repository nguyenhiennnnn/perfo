namespace Cinema.Domain.Entities;

public class ShowtimeEventTicketQuota
{
    public int ShowtimeId { get; set; }
    public int TicketTypeId { get; set; }
    public int MaxTickets { get; set; }

    public ShowtimeEventSetting EventSetting { get; set; } = null!;
    public TicketType TicketType { get; set; } = null!;
}
