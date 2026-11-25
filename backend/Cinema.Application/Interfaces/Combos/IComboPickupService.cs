using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces.Combos;

public interface IComboPickupService
{
    Task<ComboPickupOrderDto> LookupAsync(string code, CancellationToken cancellationToken);

    Task<ComboPickupResponse> PickUpAsync(
        int staffUserId,
        ComboPickupRequest request,
        CancellationToken cancellationToken
    );
}
