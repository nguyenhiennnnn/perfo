using Cinema.Domain.Enums;
using Cinema.Application.Interfaces.Reconciliation;


namespace Cinema.Application.DTOs.Reconciliation;

public sealed record ReconciliationImportRow(
    ReconciliationGateway Gateway,
    string? TransactionCode,
    string? Amount,
    string? Fee,
    string? Status,
    string? Time,
    string? FileHash = null,
    int CreatedByUserId = 0,
    GatewayStatementLine? Line = null
);
