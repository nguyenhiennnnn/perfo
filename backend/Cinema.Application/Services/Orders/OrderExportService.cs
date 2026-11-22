using Cinema.Application.DTOs.Common;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class OrderExportService(IAppDbContext db, IExportWriter exportWriter) : IOrderExportService
{
    public string Module => ImportExportModules.Orders;

    public Task<int> CountAsync(OrderListFilter filter, CancellationToken cancellationToken) =>
        ExportLimits.CountAsync(db.Orders.AsNoTracking().ApplyFilter(filter), cancellationToken);

    public async Task<ExportFileDto> ExportAsync(
        OrderListFilter filter,
        ExportFormat format,
        ExportContext context,
        IJobProgressReporter? progress,
        CancellationToken cancellationToken
    )
    {
        var query = db.Orders.AsNoTracking().ApplyFilter(filter);
        await ExportLimits.EnsureWithinLimitAsync(query, cancellationToken);

        var rows = query
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.OrderId)
            .Take(ExportLimits.MaxRows)
            .Select(o => new OrderExportRow(
                o.OrderCode,
                o.Channel,
                o.Type,
                o.CustomerName ?? (o.User == null ? null : o.User.FullName),
                o.Status,
                o.TotalAmount,
                o.DiscountAmount,
                o.FinalAmount,
                o.PaymentTransactions
                    .Where(t =>
                        t.Type == PaymentTransactionType.Charge
                        && t.Status == PaymentStatus.Success
                    )
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
                o.PaymentTransactions
                    .Where(t =>
                        t.Type == PaymentTransactionType.Refund
                        && t.Status == PaymentStatus.Success
                    )
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
                o.CreatedAt,
                o.CancelReason,
                o.CancelledBy
            ))
            .AsAsyncEnumerable();

        return await ExportFileBuilder.BuildAsync(
            exportWriter,
            Module,
            Messages.Export_SheetOrders,
            format,
            Columns(),
            rows,
            cancellationToken,
            progress
        );
    }

    private static IReadOnlyList<ExportColumn<OrderExportRow>> Columns() =>
        [
            new(Messages.Export_ColOrderCode, x => x.OrderCode),
            new(Messages.Export_ColChannel, x => ExportLabels.Channel(x.Channel)),
            new(Messages.Export_ColOrderType, x => ExportLabels.OrderKind(x.Type)),
            new(Messages.Export_ColCustomer, x => x.Customer),
            new(Messages.Export_ColStatus, x => ExportLabels.Order(x.Status)),
            new(Messages.Export_ColGrossAmount, x => x.TotalAmount, ExportColumnType.Money),
            new(Messages.Export_ColDiscount, x => x.DiscountAmount, ExportColumnType.Money),
            new(Messages.Export_ColPayableAmount, x => x.FinalAmount, ExportColumnType.Money),
            new(Messages.Export_ColPaidAmount, x => x.PaidAmount, ExportColumnType.Money),
            new(Messages.Export_ColRefundedTotal, x => x.RefundedAmount, ExportColumnType.Money),
            new(Messages.Export_ColCreatedAt, x => x.CreatedAt, ExportColumnType.DateTime),
            new(Messages.Export_ColCancelReason, x => ExportLabels.CancelReasonLabel(x.CancelReason)),
            new(Messages.Export_ColCancelledBy, x => ExportLabels.CancelledByLabel(x.CancelledBy)),
        ];

    private sealed record OrderExportRow(
        string OrderCode,
        SalesChannel Channel,
        OrderType Type,
        string? Customer,
        OrderStatus Status,
        decimal TotalAmount,
        decimal DiscountAmount,
        decimal FinalAmount,
        decimal PaidAmount,
        decimal RefundedAmount,
        DateTime CreatedAt,
        CancelReason? CancelReason,
        CancelledBy? CancelledBy
    );
}
