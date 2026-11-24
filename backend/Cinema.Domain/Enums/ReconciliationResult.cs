namespace Cinema.Domain.Enums;

public enum ReconciliationResult
{
    Matched,
    MissingInSystem,
    MissingAtGateway,
    AmountMismatch,
    StatusMismatch,
    PaidAtGatewayOrderUnconfirmed,
}
