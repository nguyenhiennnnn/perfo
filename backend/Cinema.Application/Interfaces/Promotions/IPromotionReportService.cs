using Cinema.Application.DTOs.Promotions;

namespace Cinema.Application.Interfaces.Promotions;

public interface IPromotionReportService
{
    Task<PromotionReportListDto> GetListAsync(
        PromotionReportFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<PromotionReportDto> GetAsync(
        int id,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken
    );
}
