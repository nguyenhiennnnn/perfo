using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Reconciliation;

public record ReconciliationSessionListFilter(
    ReconciliationGateway? Gateway,
    DateOnly? FromDate,
    DateOnly? ToDate
);

public record ReconciliationItemListFilter(
    int? SessionId,
    ReconciliationResult? Result,
    ReconciliationResolution? Resolution,
    int? AssignedToUserId,
    ReconciliationGateway? Gateway
);

public record ReconciliationSessionDto(
    int SessionId,
    string Gateway,
    string Source,
    string? FileName,
    DateOnly? PeriodFrom,
    DateOnly? PeriodTo,
    string Status,
    int TotalRows,
    int MatchedCount,
    int MissingInSystemCount,
    int MissingAtGatewayCount,
    int AmountMismatchCount,
    int StatusMismatchCount,
    int PaidAtGatewayOrderUnconfirmedCount,
    decimal GatewayTotalAmount,
    decimal SystemTotalAmount,
    int OpenItemCount,
    int ResolvedItemCount,
    int IgnoredItemCount,
    string? ErrorMessage,
    int CreatedByUserId,
    string? CreatedByName,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

public record ReconciliationItemDto(
    int ItemId,
    int SessionId,
    string Gateway,
    string GatewayTransactionCode,
    decimal? GatewayAmount,
    decimal GatewayFee,
    string? GatewayStatus,
    DateTime? GatewayTime,
    int? PaymentTransactionId,
    string? PaymentTransactionCode,
    int? OrderId,
    string? OrderCode,
    decimal? SystemAmount,
    string Result,
    string Resolution,
    string? Note,
    int? AssignedToUserId,
    string? AssignedToName,
    DateTime? ResolvedAt,
    int? ResolvedByUserId,
    string? ResolvedByName
);

public record UpdateReconciliationItemRequest(
    string? Note,
    int? AssignedToUserId,
    bool ClearAssignee,
    ReconciliationResolution? Resolution
);
