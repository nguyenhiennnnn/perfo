namespace Cinema.Application.DTOs.CounterShifts;

public record CounterShiftDto(
    int CounterShiftId,
    int EmployeeId,
    string EmployeeName,
    DateTime OpenedAt,
    decimal OpeningCash,
    DateTime? ClosedAt,
    decimal? CountedCash,
    decimal? ExpectedCash,
    decimal? Variance,
    string? Note,
    string Status
);

public record OpenCounterShiftRequest(decimal OpeningCash);

public record CloseCounterShiftRequest(decimal CountedCash, string? Note);

public record AdminCloseCounterShiftRequest(decimal CountedCash, string Note);

public record OpenCounterShiftItemDto(CounterShiftDto Shift, bool IsOverdue, DateTime? AlertedAt);

public record CounterShiftListFilter(
    int? EmployeeId,
    string? Status,
    DateOnly? FromDate,
    DateOnly? ToDate
);

public record CounterShiftActor(int UserId, int? EmployeeId, bool IsAdmin);
