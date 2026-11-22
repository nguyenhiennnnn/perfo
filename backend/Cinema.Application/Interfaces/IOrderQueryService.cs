using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces;

public record OrderListFilter(
    string? Status,
    string? Channel,
    string? Type,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Search
);

public interface IOrderQueryService
{
    Task<CursorPageResult<OrderListItemDto>> GetMyOrdersAsync(
        int userId,
        OrderListFilter filter,
        string? cursor,
        int limit,
        CancellationToken cancellationToken
    );

    Task<OrderDetailDto> GetMyOrderDetailAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken
    );

    Task<OffsetPageResult<OrderListItemDto>> GetOrdersAdminAsync(
        OrderListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<OrderDetailDto> GetOrderDetailAdminAsync(
        int orderId,
        CancellationToken cancellationToken
    );
}
