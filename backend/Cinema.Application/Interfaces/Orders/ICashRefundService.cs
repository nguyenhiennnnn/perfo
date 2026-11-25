using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Payments;

namespace Cinema.Application.Interfaces.Orders;

public static class CashRefundStatuses
{
    public const string Pending = "Pending";
    public const string Refunded = "Refunded";
    public const string All = "All";
}

public record CashRefundListFilter(
    string? Status,
    string? OrderCode,
    DateOnly? FromDate,
    DateOnly? ToDate
);

public interface ICashRefundService
{
    Task<OffsetPageResult<CashRefundListItemDto>> GetCashRefundsAsync(
        CashRefundListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<CashRefundConfirmResultDto> ConfirmAsync(
        int refundTransactionId,
        int confirmedByUserId,
        CancellationToken cancellationToken
    );
}
