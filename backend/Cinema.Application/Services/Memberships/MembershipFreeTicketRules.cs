using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Memberships;

public static class MembershipFreeTicketRules
{
    public static bool AllowsShowtime(ShowtimeKind kind, bool eventAllowsFreeTickets) =>
        kind switch
        {
            ShowtimeKind.Regular => true,
            ShowtimeKind.Event => eventAllowsFreeTickets,
            _ => false,
        };
}
