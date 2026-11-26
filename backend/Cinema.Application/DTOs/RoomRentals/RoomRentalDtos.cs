using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.RoomRentals;

public record CreateRoomRentalRequestRequest(
    string FullName,
    string? OrganizationName,
    RoomRentalAudienceType AudienceType,
    string ContactPhone,
    string ContactEmail,
    DateTime DesiredStartAt,
    DateTime? FlexibleFromAt,
    DateTime? FlexibleToAt,
    int AttendeeCount,
    int? MovieId,
    bool BringsOwnContent,
    string? ContentNote,
    int? ScreenTypeId,
    string? ComboRequest,
    string? Notes
);

public record RoomRentalTrackingRequest(string Token);

public record RoomRentalTrackingDto(
    string RequestCode,
    RoomRentalRequestStatus Status,
    string FullName,
    string? OrganizationName,
    RoomRentalAudienceType AudienceType,
    DateTime DesiredStartAt,
    DateTime? FlexibleFromAt,
    DateTime? FlexibleToAt,
    int AttendeeCount,
    string? MovieTitle,
    bool BringsOwnContent,
    string? ContentNote,
    string? ScreenTypeName,
    string? ComboRequest,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime TrackingExpiresAt
);

public record RoomRentalAdminListFilter(RoomRentalRequestStatus? Status, string? Search);

public record RoomRentalAdminListItemDto(
    int RoomRentalRequestId,
    string RequestCode,
    string FullName,
    string? OrganizationName,
    RoomRentalAudienceType AudienceType,
    string ContactPhone,
    DateTime DesiredStartAt,
    int AttendeeCount,
    RoomRentalRequestStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record RoomRentalAdminDto(
    int RoomRentalRequestId,
    string RequestCode,
    string FullName,
    string? OrganizationName,
    RoomRentalAudienceType AudienceType,
    string ContactPhone,
    string ContactEmail,
    DateTime DesiredStartAt,
    DateTime? FlexibleFromAt,
    DateTime? FlexibleToAt,
    int AttendeeCount,
    int? MovieId,
    string? MovieTitle,
    bool BringsOwnContent,
    string? ContentNote,
    int? ScreenTypeId,
    string? ScreenTypeName,
    string? ComboRequest,
    string? Notes,
    RoomRentalRequestStatus Status,
    string? StatusNote,
    IReadOnlyList<RoomRentalRequestStatus> AllowedNextStatuses,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ChangeRoomRentalRequestStatusRequest(RoomRentalRequestStatus Status, string? Note);

public record RoomRentalRequestReceiptDto(
    string RequestCode,
    RoomRentalRequestStatus Status,
    DateTime CreatedAt
);
