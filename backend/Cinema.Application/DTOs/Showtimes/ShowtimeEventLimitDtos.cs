namespace Cinema.Application.DTOs.Showtimes;

public record ShowtimeEventTicketQuotaRequest(int TicketTypeId, int MaxTickets);

public record UpsertShowtimeEventLimitsRequest(
    int? MaxTicketsPerCustomer,
    List<ShowtimeEventTicketQuotaRequest>? TicketQuotas
);

public record ShowtimeEventTicketQuotaDto(
    int TicketTypeId,
    string TicketTypeName,
    int MaxTickets,
    int UsedTickets
);

public record ShowtimeEventLimitsDto(
    int ShowtimeId,
    int? MaxTicketsPerCustomer,
    IReadOnlyList<ShowtimeEventTicketQuotaDto> TicketQuotas
);
