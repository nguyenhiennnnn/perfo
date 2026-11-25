using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Interfaces.ImportExport;


namespace Cinema.Application.Interfaces.Promotions;

public interface IPromotionRedemptionExportService : IExportSource<PromotionRedemptionListFilter>;
