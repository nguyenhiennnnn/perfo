namespace Cinema.Application.DTOs.Showtimes;

public record UpsertShowtimeEarlyAccessRequest(
    DateTime GeneralSaleStartTime,
    List<int>? MembershipTierIds,
    bool PaidMembers,
    bool MovieInterest
);

public record ShowtimeEarlyAccessSettingDto(
    int ShowtimeId,
    DateTime? GeneralSaleStartTime,
    IReadOnlyList<int> MembershipTierIds,
    bool PaidMembers,
    bool MovieInterest
);

public record ShowtimeEarlyAccessDto(
    DateTime GeneralSaleStartTime,
    bool CanBook,
    string? IneligibleReason,
    IReadOnlyList<string> MembershipTierNames,
    bool PaidMembers,
    bool MovieInterest
);
