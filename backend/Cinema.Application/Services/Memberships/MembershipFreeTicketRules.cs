using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Memberships;

public static class MembershipFreeTicketRules
{
    public static bool AllowsShowtime(ShowtimeKind kind) => kind == ShowtimeKind.Regular;
}
