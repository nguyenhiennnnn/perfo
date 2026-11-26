namespace Cinema.Application.Services.RoomRentals;

public static class RoomRentalTrackingLink
{
    public static string Build(string frontendBaseUrl, string rawToken) =>
        $"{frontendBaseUrl.TrimEnd('/')}/room-rental/track?token={Uri.EscapeDataString(rawToken)}";
}
