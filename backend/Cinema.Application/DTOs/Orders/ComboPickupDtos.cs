namespace Cinema.Application.DTOs.Orders;

public record ComboPickupItemRequest(int OrderLineId, int Quantity);

public record ComboPickupRequest(string Code, List<ComboPickupItemRequest>? Items);

public record ComboPickupLineDto(
    int OrderLineId,
    int? ComboId,
    string? ComboName,
    int Quantity,
    int RefundedQuantity,
    int QuantityFulfilled,
    int RemainingQuantity
);

public record ComboPickupHistoryDto(
    long ComboPickupId,
    int OrderLineId,
    string? ComboName,
    int Quantity,
    DateTime PickedUpAt,
    int StaffUserId,
    string? StaffName
);

public record ComboPickupOrderDto(
    int OrderId,
    string OrderCode,
    string OrderType,
    string Status,
    string? CustomerName,
    string? CustomerPhone,
    DateTime CreatedAt,
    bool IsComboOnly,
    bool IsUnclaimed,
    bool IsFullyFulfilled,
    int TotalRemaining,
    List<ComboPickupLineDto> Lines,
    List<ComboPickupHistoryDto> History
);

public record ComboPickupResponse(
    ComboPickupOrderDto Order,
    int QuantityPickedUp,
    DateTime PickedUpAt
);

public record UnclaimedComboLineDto(string? ComboName, int Quantity, int RemainingQuantity);

public record UnclaimedComboOrderDto(
    int OrderId,
    string OrderCode,
    string? CustomerName,
    string? CustomerPhone,
    DateTime CreatedAt,
    DateOnly SaleDate,
    int DaysOverdue,
    decimal FinalAmount,
    int TotalQuantity,
    int FulfilledQuantity,
    int RemainingQuantity,
    List<UnclaimedComboLineDto> Lines
);
