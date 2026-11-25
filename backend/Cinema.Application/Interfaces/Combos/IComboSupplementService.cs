using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces.Combos;

public interface IComboSupplementService
{
    Task<HoldOrderResponse> CreateAsync(
        int userId,
        int originalOrderId,
        ComboSupplementRequest request,
        CancellationToken cancellationToken
    );
}
