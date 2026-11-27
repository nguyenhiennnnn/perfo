using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Orders;

public interface IOrderReleaseService
{
    Task<bool> ExpireAsync(int orderId, CancellationToken cancellationToken);

    Task<bool> CancelPendingAsync(
        int orderId,
        CancelReason reason,
        CancelledBy cancelledBy,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Như <see cref="CancelPendingAsync"/> nhưng không tự đặt chỗ hoàn khoản đã thu: dành cho
    /// luồng đã tự quyết định số hoàn (vd. thuê phòng giữ lại một phần tiền cọc) trước khi gọi.
    /// </summary>
    Task<bool> CancelPendingWithoutRefundAsync(
        int orderId,
        CancelReason reason,
        CancelledBy cancelledBy,
        CancellationToken cancellationToken
    );
}
