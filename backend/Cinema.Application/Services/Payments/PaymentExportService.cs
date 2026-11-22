using Cinema.Application.DTOs.Common;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Payments;

public class PaymentExportService(IAppDbContext db, IExportWriter exportWriter)
    : IPaymentExportService
{
    public string Module => ImportExportModules.Payments;

    public Task<int> CountAsync(
        AdminPaymentListFilter filter,
        CancellationToken cancellationToken
    ) =>
        ExportLimits.CountAsync(
            db.PaymentTransactions.AsNoTracking().ApplyAdminFilter(filter),
            cancellationToken
        );

    public async Task<ExportFileDto> ExportAsync(
        AdminPaymentListFilter filter,
        ExportFormat format,
        ExportContext context,
        IJobProgressReporter? progress,
        CancellationToken cancellationToken
    )
    {
        var query = db.PaymentTransactions.AsNoTracking().ApplyAdminFilter(filter);
        await ExportLimits.EnsureWithinLimitAsync(query, cancellationToken);

        var rows = query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.PaymentTransactionId)
            .Take(ExportLimits.MaxRows)
            .Select(t => new TransactionExportRow(
                t.TransactionCode,
                t.Order.OrderCode,
                t.Type,
                t.Method,
                t.Amount,
                t.GatewayFee,
                t.Status,
                t.CreatedAt,
                t.OriginalTransaction == null ? null : t.OriginalTransaction.TransactionCode
            ))
            .AsAsyncEnumerable();

        return await ExportFileBuilder.BuildAsync(
            exportWriter,
            Module,
            Messages.Export_SheetPayments,
            format,
            Columns(),
            rows,
            cancellationToken,
            progress
        );
    }

    private static IReadOnlyList<ExportColumn<TransactionExportRow>> Columns() =>
        [
            new(Messages.Export_ColTransactionCode, x => x.TransactionCode),
            new(Messages.Export_ColOrderCode, x => x.OrderCode),
            new(Messages.Export_ColTransactionType, x => ExportLabels.TransactionKind(x.Type)),
            new(Messages.Export_ColPaymentMethod, x => ExportLabels.Method(x.Method)),
            new(Messages.Export_ColAmount, x => x.Amount, ExportColumnType.Money),
            new(Messages.Export_ColGatewayFee, x => x.GatewayFee, ExportColumnType.Money),
            new(Messages.Export_ColStatus, x => ExportLabels.Payment(x.Status)),
            new(Messages.Export_ColTransactionTime, x => x.CreatedAt, ExportColumnType.DateTime),
            new(Messages.Export_ColOriginalTransaction, x => x.OriginalTransactionCode),
        ];

    private sealed record TransactionExportRow(
        string TransactionCode,
        string OrderCode,
        PaymentTransactionType Type,
        PaymentMethod Method,
        decimal Amount,
        decimal GatewayFee,
        PaymentStatus Status,
        DateTime CreatedAt,
        string? OriginalTransactionCode
    );
}
