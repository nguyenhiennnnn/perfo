using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Promotions;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Promotions;

public class PromotionRedemptionExportService(IAppDbContext db, IExportWriter exportWriter)
    : IPromotionRedemptionExportService
{
    public string Module => ImportExportModules.PromotionRedemptions;

    public Task<int> CountAsync(
        PromotionRedemptionListFilter filter,
        CancellationToken cancellationToken
    ) =>
        ExportLimits.CountAsync(
            db.PromotionRedemptions.AsNoTracking().ApplyFilter(filter),
            cancellationToken
        );

    public async Task<ExportFileDto> ExportAsync(
        PromotionRedemptionListFilter filter,
        ExportFormat format,
        ExportContext context,
        IJobProgressReporter? progress,
        CancellationToken cancellationToken
    )
    {
        var query = db.PromotionRedemptions.AsNoTracking().ApplyFilter(filter);
        await ExportLimits.EnsureWithinLimitAsync(query, cancellationToken);

        var rows = query
            .OrderByDescending(r => r.ReservedAt)
            .ThenByDescending(r => r.PromotionRedemptionId)
            .Take(ExportLimits.MaxRows)
            .Select(r => new RedemptionExportRow(
                r.Promotion.Code,
                r.Order.OrderCode,
                r.Order.Channel,
                r.Order.CustomerName ?? (r.User == null ? null : r.User.FullName),
                r.Status,
                r.DiscountAmount,
                r.Order.FinalAmount,
                r.ReservedAt,
                r.RedeemedAt,
                r.ReleasedAt
            ))
            .AsAsyncEnumerable();

        return await ExportFileBuilder.BuildAsync(
            exportWriter,
            Module,
            Messages.Export_SheetPromotionRedemptions,
            format,
            Columns(),
            rows,
            cancellationToken,
            progress
        );
    }

    private static IReadOnlyList<ExportColumn<RedemptionExportRow>> Columns() =>
        [
            new(Messages.Export_ColPromotionCode, x => x.PromotionCode),
            new(Messages.Export_ColOrderCode, x => x.OrderCode),
            new(Messages.Export_ColChannel, x => ExportLabels.Channel(x.Channel)),
            new(Messages.Export_ColCustomer, x => x.Customer),
            new(Messages.Export_ColStatus, x => ExportLabels.PromotionRedemption(x.Status)),
            new(Messages.Export_ColDiscount, x => x.DiscountAmount, ExportColumnType.Money),
            new(Messages.Export_ColPayableAmount, x => x.OrderFinalAmount, ExportColumnType.Money),
            new(Messages.Export_ColReservedAt, x => x.ReservedAt, ExportColumnType.DateTime),
            new(Messages.Export_ColRedeemedAt, x => x.RedeemedAt, ExportColumnType.DateTime),
            new(Messages.Export_ColReleasedAt, x => x.ReleasedAt, ExportColumnType.DateTime),
        ];

    private sealed record RedemptionExportRow(
        string PromotionCode,
        string OrderCode,
        SalesChannel Channel,
        string? Customer,
        PromotionRedemptionStatus Status,
        decimal DiscountAmount,
        decimal OrderFinalAmount,
        DateTime ReservedAt,
        DateTime? RedeemedAt,
        DateTime? ReleasedAt
    );
}
