using Cinema.Application.Caching;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reconciliation;

public class ReconciliationService(
    IAppDbContext db,
    IAppCache cache,
    IAuditLogger auditLogger,
    ISalesStatsService salesStats,
    IReconciliationAnomalyHook anomalyHook
) : IReconciliationService
{
    private const int LookupChunkSize = 500;

    public async Task<ReconciliationRunResult> RunAsync(
        ReconciliationRequest request,
        CancellationToken cancellationToken
    )
    {
        var lines = Validate(request);
        var (session, reused) = await StartSessionAsync(request, cancellationToken);
        try
        {
            return await ExecuteAsync(session, reused, request, lines, cancellationToken);
        }
        catch (Exception ex)
        {
            await MarkFailedAsync(session.ReconciliationSessionId, ex);
            throw;
        }
    }

    private static List<GatewayStatementLine> Validate(ReconciliationRequest request)
    {
        if (request.PeriodFrom > request.PeriodTo)
            throw new ValidationAppException(Messages.Invalid_Reconciliation_Period);
        if (request.Source == ReconciliationSource.File && string.IsNullOrWhiteSpace(request.FileHash))
            throw new ValidationAppException(Messages.Invalid_Reconciliation_FileHash);

        var result = new List<GatewayStatementLine>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in request.Lines)
        {
            var code = raw.TransactionCode?.Trim();
            if (
                string.IsNullOrEmpty(code)
                || code.Length > FieldLengths.TransactionCode
                || raw.Amount < 0
                || raw.Fee < 0
            )
                throw new ValidationAppException(Messages.Invalid_Reconciliation_Line);
            if (!seen.Add(code))
                continue;
            var status = raw.Status?.Trim();
            if (status is { Length: > FieldLengths.GatewayStatus })
                status = status[..FieldLengths.GatewayStatus];
            result.Add(raw with { TransactionCode = code, Status = status });
        }
        return result;
    }

    private async Task<(ReconciliationSession Session, bool Reused)> StartSessionAsync(
        ReconciliationRequest request,
        CancellationToken cancellationToken
    )
    {
        ReconciliationSession? session = null;
        if (request.Source == ReconciliationSource.File)
        {
            var hash = request.FileHash!.Trim();
            session = await db.ReconciliationSessions.FirstOrDefaultAsync(
                s =>
                    s.Gateway == request.Gateway
                    && s.Source == ReconciliationSource.File
                    && s.FileHash == hash,
                cancellationToken
            );
        }
        else
        {
            session = await db.ReconciliationSessions.FirstOrDefaultAsync(
                s =>
                    s.Gateway == request.Gateway
                    && s.Source == ReconciliationSource.Api
                    && s.PeriodFrom == request.PeriodFrom
                    && s.PeriodTo == request.PeriodTo,
                cancellationToken
            );
        }

        var reused = session is not null;
        if (session is null)
        {
            session = new ReconciliationSession
            {
                Gateway = request.Gateway,
                Source = request.Source,
                FileHash = request.Source == ReconciliationSource.File ? request.FileHash!.Trim() : null,
                CreatedByUserId = request.CreatedByUserId,
                CreatedAt = DateTime.UtcNow,
            };
            db.ReconciliationSessions.Add(session);
        }

        session.FileName = request.FileName;
        session.PeriodFrom = request.PeriodFrom;
        session.PeriodTo = request.PeriodTo;
        session.Status = ReconciliationStatus.Running;
        session.ErrorMessage = null;
        session.CompletedAt = null;
        await db.SaveChangesAsync(cancellationToken);
        return (session, reused);
    }

    private async Task MarkFailedAsync(int sessionId, Exception ex)
    {
        var message = ex.Message.Length > FieldLengths.ErrorMessage
            ? ex.Message[..FieldLengths.ErrorMessage]
            : ex.Message;
        var now = DateTime.UtcNow;
        await db
            .ReconciliationSessions.Where(s => s.ReconciliationSessionId == sessionId)
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(x => x.Status, ReconciliationStatus.Failed)
                        .SetProperty(x => x.ErrorMessage, message)
                        .SetProperty(x => x.CompletedAt, now),
                CancellationToken.None
            );
    }

    private async Task<ReconciliationRunResult> ExecuteAsync(
        ReconciliationSession session,
        bool reused,
        ReconciliationRequest request,
        List<GatewayStatementLine> lines,
        CancellationToken cancellationToken
    )
    {
        var method = ToMethod(request.Gateway);
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var fromUtc = BusinessDay.StartUtc(request.PeriodFrom, settings.OpeningTime, settings.ClosingTime);
        var toUtc = BusinessDay.EndUtc(request.PeriodTo, settings.OpeningTime, settings.ClosingTime);

        var txs = await LoadTransactionsAsync(method, lines, cancellationToken);
        var byCode = txs.ToDictionary(t => t.TransactionCode, StringComparer.Ordinal);
        var byGatewayId = txs
            .Where(t => t.GatewayTransactionId != null)
            .ToDictionary(t => t.GatewayTransactionId!, StringComparer.Ordinal);
        var netCancelled = await LoadCancelledNetAsync(txs, cancellationToken);

        var desired = new List<DesiredItem>();
        var matchedTxIds = new HashSet<int>();
        var feeDays = new HashSet<DateOnly>();
        var feesUpdated = 0;

        foreach (var line in lines)
        {
            var tx = byCode.GetValueOrDefault(line.TransactionCode) ?? byGatewayId.GetValueOrDefault(line.TransactionCode);
            var result = Classify(line, tx, netCancelled);
            if (tx is not null)
                matchedTxIds.Add(tx.PaymentTransactionId);
            if (
                result == ReconciliationResult.Matched
                && tx is { Status: PaymentStatus.Success }
                && line.Fee > 0
                && tx.GatewayFee != line.Fee
            )
            {
                auditLogger.Record(
                    AuditActions.Update,
                    AuditEntityTypes.PaymentTransaction,
                    tx.PaymentTransactionId.ToString(),
                    new { tx.GatewayFee },
                    new
                    {
                        GatewayFee = line.Fee,
                        Source = "Reconciliation",
                        session.ReconciliationSessionId,
                    }
                );
                tx.GatewayFee = line.Fee;
                feesUpdated++;
                if (tx.CompletedAt is { } completed)
                    feeDays.Add(BusinessDay.Of(completed, settings.OpeningTime, settings.ClosingTime));
            }
            desired.Add(new DesiredItem(line.TransactionCode, line, tx?.PaymentTransactionId, result));
        }

        var missing = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.Method == method
                && t.Status == PaymentStatus.Success
                && t.CompletedAt >= fromUtc
                && t.CompletedAt < toUtc
                && !matchedTxIds.Contains(t.PaymentTransactionId)
            )
            .ToListAsync(cancellationToken);
        var usedCodes = desired.Select(d => d.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var tx in missing)
        {
            var code = tx.GatewayTransactionId ?? tx.TransactionCode;
            if (usedCodes.Add(code))
                desired.Add(
                    new DesiredItem(code, null, tx.PaymentTransactionId, ReconciliationResult.MissingAtGateway)
                );
        }

        var systemTotal =
            await db
                .PaymentTransactions.Where(t =>
                    t.Method == method
                    && t.Status == PaymentStatus.Success
                    && t.CompletedAt >= fromUtc
                    && t.CompletedAt < toUtc
                )
                .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var items = await SyncItemsAsync(session, desired, cancellationToken);
        session.TotalRows = lines.Count;
        session.GatewayTotalAmount = lines.Where(l => l.IsSuccess).Sum(l => l.Amount);
        session.SystemTotalAmount = systemTotal;
        await db.SaveChangesAsync(cancellationToken);

        if (feeDays.Count > 0)
            await salesStats.RefreshPaymentDaysAsync(feeDays, cancellationToken);

        foreach (
            var item in items.Where(i =>
                i.Result == ReconciliationResult.PaidAtGatewayOrderUnconfirmed
                && i.Resolution == ReconciliationResolution.Open
            )
        )
            await anomalyHook.OnAnomalyAsync(item, cancellationToken);

        var counts = await db
            .ReconciliationItems.Where(i => i.ReconciliationSessionId == session.ReconciliationSessionId)
            .GroupBy(i => i.Result)
            .Select(g => new { Result = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Result, x => x.Count, cancellationToken);
        int Count(ReconciliationResult r) => counts.GetValueOrDefault(r);

        session.MatchedCount = Count(ReconciliationResult.Matched);
        session.MissingInSystemCount = Count(ReconciliationResult.MissingInSystem);
        session.MissingAtGatewayCount = Count(ReconciliationResult.MissingAtGateway);
        session.AmountMismatchCount = Count(ReconciliationResult.AmountMismatch);
        session.StatusMismatchCount = Count(ReconciliationResult.StatusMismatch);
        session.PaidAtGatewayOrderUnconfirmedCount = Count(ReconciliationResult.PaidAtGatewayOrderUnconfirmed);
        session.Status = ReconciliationStatus.Completed;
        session.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new ReconciliationRunResult(
            session.ReconciliationSessionId,
            reused,
            session.TotalRows,
            session.MatchedCount,
            session.MissingInSystemCount,
            session.MissingAtGatewayCount,
            session.AmountMismatchCount,
            session.StatusMismatchCount,
            session.PaidAtGatewayOrderUnconfirmedCount,
            session.GatewayTotalAmount,
            session.SystemTotalAmount,
            feesUpdated
        );
    }

    private async Task<List<PaymentTransaction>> LoadTransactionsAsync(
        PaymentMethod method,
        List<GatewayStatementLine> lines,
        CancellationToken cancellationToken
    )
    {
        var result = new Dictionary<int, PaymentTransaction>();
        foreach (var chunk in lines.Select(l => l.TransactionCode).Chunk(LookupChunkSize))
        {
            var codes = chunk.ToList();
            var found = await db
                .PaymentTransactions.Include(t => t.Order)
                .Where(t =>
                    t.Method == method
                    && (
                        codes.Contains(t.TransactionCode)
                        || (t.GatewayTransactionId != null && codes.Contains(t.GatewayTransactionId))
                    )
                )
                .ToListAsync(cancellationToken);
            foreach (var t in found)
                result[t.PaymentTransactionId] = t;
        }
        return [.. result.Values];
    }

    private async Task<Dictionary<int, decimal>> LoadCancelledNetAsync(
        List<PaymentTransaction> txs,
        CancellationToken cancellationToken
    )
    {
        var orderIds = txs.Where(t => t.Order.Status == OrderStatus.Cancelled)
            .Select(t => t.OrderId)
            .Distinct()
            .ToList();
        if (orderIds.Count == 0)
            return [];
        return await db
            .PaymentTransactions.Where(t => orderIds.Contains(t.OrderId) && t.Status == PaymentStatus.Success)
            .GroupBy(t => t.OrderId)
            .Select(g => new
            {
                OrderId = g.Key,
                Net = g.Sum(t => t.Type == PaymentTransactionType.Charge ? t.Amount : -t.Amount),
            })
            .ToDictionaryAsync(x => x.OrderId, x => x.Net, cancellationToken);
    }

    private static ReconciliationResult Classify(
        GatewayStatementLine line,
        PaymentTransaction? tx,
        Dictionary<int, decimal> netCancelled
    )
    {
        if (tx is null)
            return ReconciliationResult.MissingInSystem;
        if (line.IsSuccess && IsOrderUnconfirmed(tx, netCancelled))
            return ReconciliationResult.PaidAtGatewayOrderUnconfirmed;
        var systemSuccess = tx.Status == PaymentStatus.Success;
        if (line.IsSuccess != systemSuccess)
            return ReconciliationResult.StatusMismatch;
        if (systemSuccess && line.Amount != tx.Amount)
            return ReconciliationResult.AmountMismatch;
        return ReconciliationResult.Matched;
    }

    private static bool IsOrderUnconfirmed(PaymentTransaction tx, Dictionary<int, decimal> netCancelled) =>
        tx.Type == PaymentTransactionType.Charge
        && tx.Order.Status switch
        {
            OrderStatus.Pending or OrderStatus.Expired => true,
            OrderStatus.Cancelled => netCancelled.GetValueOrDefault(tx.OrderId) > 0,
            _ => false,
        };

    private async Task<List<ReconciliationItem>> SyncItemsAsync(
        ReconciliationSession session,
        List<DesiredItem> desired,
        CancellationToken cancellationToken
    )
    {
        var existing = await db
            .ReconciliationItems.Where(i => i.ReconciliationSessionId == session.ReconciliationSessionId)
            .ToListAsync(cancellationToken);
        var byCode = existing.ToDictionary(i => i.GatewayTransactionCode, StringComparer.Ordinal);
        var result = new List<ReconciliationItem>(desired.Count);

        foreach (var d in desired)
        {
            if (!byCode.TryGetValue(d.Code, out var item))
            {
                item = new ReconciliationItem
                {
                    ReconciliationSessionId = session.ReconciliationSessionId,
                    GatewayTransactionCode = d.Code,
                    Gateway = session.Gateway,
                };
                db.ReconciliationItems.Add(item);
            }
            else if (
                item.Result != d.Result
                && item.Resolution != ReconciliationResolution.Open
                && d.Result != ReconciliationResult.Matched
            )
            {
                item.Resolution = ReconciliationResolution.Open;
                item.ResolvedAt = null;
                item.ResolvedByUserId = null;
            }

            item.Amount = d.Line?.Amount;
            item.GatewayFee = d.Line?.Fee ?? 0m;
            item.GatewayStatus = d.Line?.Status;
            item.GatewayTime = d.Line?.TimeUtc;
            item.PaymentTransactionId = d.PaymentTransactionId;
            item.Result = d.Result;
            result.Add(item);
        }

        var desiredCodes = desired.Select(d => d.Code).ToHashSet(StringComparer.Ordinal);
        db.ReconciliationItems.RemoveRange(
            existing.Where(i =>
                !desiredCodes.Contains(i.GatewayTransactionCode) && i.Resolution == ReconciliationResolution.Open
            )
        );
        return result;
    }

    private static PaymentMethod ToMethod(ReconciliationGateway gateway) =>
        gateway switch
        {
            ReconciliationGateway.VNPay => PaymentMethod.VNPay,
            ReconciliationGateway.Momo => PaymentMethod.Momo,
            ReconciliationGateway.Stripe => PaymentMethod.Stripe,
            _ => throw new ArgumentOutOfRangeException(nameof(gateway)),
        };

    private sealed record DesiredItem(
        string Code,
        GatewayStatementLine? Line,
        int? PaymentTransactionId,
        ReconciliationResult Result
    );
}
