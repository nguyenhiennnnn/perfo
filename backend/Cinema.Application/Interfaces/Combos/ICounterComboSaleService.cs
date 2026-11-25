using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces.Combos;

public interface ICounterComboSaleService
{
    Task<CounterComboSaleResponse> SellAsync(
        int employeeId,
        int employeeUserId,
        CounterComboSaleRequest request,
        CancellationToken cancellationToken
    );
}
