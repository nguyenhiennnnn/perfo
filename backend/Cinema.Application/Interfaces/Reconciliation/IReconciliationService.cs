using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Reconciliation;

public sealed record GatewayStatementLine(
    string TransactionCode,
    decimal Amount,
    decimal Fee,
    string? Status,
    bool IsSuccess,
    DateTime? TimeUtc
);

public sealed record ReconciliationRequest(
    ReconciliationGateway Gateway,
    ReconciliationSource Source,
    string? FileName,
    string? FileHash,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    IReadOnlyList<GatewayStatementLine> Lines,
    int CreatedByUserId
);

public sealed record ReconciliationRunResult(
    int SessionId,
    bool Reused,
    int TotalRows,
    int MatchedCount,
    int MissingInSystemCount,
    int MissingAtGatewayCount,
    int AmountMismatchCount,
    int StatusMismatchCount,
    int PaidAtGatewayOrderUnconfirmedCount,
    decimal GatewayTotalAmount,
    decimal SystemTotalAmount,
    int FeesUpdated
);

public interface IReconciliationService
{
    Task<ReconciliationRunResult> RunAsync(
        ReconciliationRequest request,
        CancellationToken cancellationToken
    );
}
