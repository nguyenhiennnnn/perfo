using Cinema.Application.DTOs.Promotions;

namespace Cinema.Application.Interfaces;

public interface IMyPromotionService
{
    Task<List<MyPromotionDto>> GetPersonalCodesAsync(
        int userId,
        CancellationToken cancellationToken
    );
}
