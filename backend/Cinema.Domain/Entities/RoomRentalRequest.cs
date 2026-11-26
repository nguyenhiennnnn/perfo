using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class RoomRentalRequest
{
    public int RoomRentalRequestId { get; set; }
    public string RequestCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? OrganizationName { get; set; }
    public RoomRentalAudienceType AudienceType { get; set; }
    public string ContactPhone { get; set; } = null!;
    public string ContactEmail { get; set; } = null!;
    public DateTime DesiredStartAt { get; set; }
    public DateTime? FlexibleFromAt { get; set; }
    public DateTime? FlexibleToAt { get; set; }
    public int AttendeeCount { get; set; }
    public int? MovieId { get; set; }
    public bool BringsOwnContent { get; set; }
    public string? ContentNote { get; set; }
    public int? ScreenTypeId { get; set; }
    public string? ComboRequest { get; set; }
    public string? Notes { get; set; }
    public RoomRentalRequestStatus Status { get; set; } = RoomRentalRequestStatus.New;
    public string? StatusNote { get; set; }
    public string TrackingTokenHash { get; set; } = null!;
    public DateTime TrackingTokenExpiresAt { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? ClientDeviceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Movie? Movie { get; set; }
    public ScreenType? ScreenType { get; set; }
}
