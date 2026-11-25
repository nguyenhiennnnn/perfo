using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces.Orders;

public interface IOrderComboSuggestionService
{
    Task<OrderComboSuggestionsDto> GetAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken
    );
}
