using System.Globalization;
using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Support;
using Cinema.Application.Services.Payments;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Support;

public class SupportAutoRequestService(IAppDbContext db, IAppCache cache) : ISupportAutoRequestService
{
    private static readonly NumberFormatInfo MoneyFormat = new() { NumberGroupSeparator = "." };

    public async Task<int?> EnsureForTransactionAsync(
        int paymentTransactionId,
        SupportAutoRequestReason reason,
        CancellationToken cancellationToken
    )
    {
        var existing = await FindExistingAsync(paymentTransactionId, cancellationToken);
        if (existing is not null)
            return existing;

        var source = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t => t.PaymentTransactionId == paymentTransactionId)
            .Select(t => new
            {
                t.OrderId,
                t.Amount,
                t.Method,
                t.TransactionCode,
                t.GatewayTransactionId,
                OrderCode = t.Order.OrderCode,
                OrderStatus = t.Order.Status,
                t.Order.UserId,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (source is null)
            return null;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockOrderAsync(source.OrderId, cancellationToken);

        existing = await FindExistingAsync(paymentTransactionId, cancellationToken);
        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return existing;
        }

        var reconciliation = reason == SupportAutoRequestReason.ChargedOrderNotConfirmed;
        var type =
            reconciliation || source.OrderStatus != OrderStatus.Confirmed
                ? SupportRequestType.ChargedOrderNotConfirmed
                : SupportRequestType.Other;
        var code = source.GatewayTransactionId ?? source.TransactionCode;
        var now = DateTime.UtcNow;
        var entity = new SupportRequest
        {
            OrderId = source.OrderId,
            UserId = source.UserId,
            SourcePaymentTransactionId = paymentTransactionId,
            Type = type,
            Subject = string.Format(
                reconciliation
                    ? Messages.Support_AutoSubject_ChargedNotConfirmed
                    : Messages.Support_AutoSubject_OverpaymentRefundFailed,
                source.OrderCode
            ),
            Status = SupportRequestStatus.New,
            DueAt = now.AddHours(settings.SupportRequestDueHours),
            CreatedAt = now,
            UpdatedAt = now,
            LastMessageAt = now,
            Messages =
            [
                new SupportMessage
                {
                    AuthorType = SupportMessageAuthorType.System,
                    Content = string.Format(
                        reason switch
                        {
                            SupportAutoRequestReason.ChargedOrderNotConfirmed =>
                                Messages.Support_AutoMessage_ChargedNotConfirmed,
                            SupportAutoRequestReason.GatewayRefundUnresolved =>
                                Messages.Support_AutoMessage_GatewayRefundUnresolved,
                            _ => Messages.Support_AutoMessage_OverpaymentRefundFailed,
                        },
                        code,
                        source.Method,
                        source.Amount.ToString("N0", MoneyFormat),
                        source.OrderCode
                    ),
                    CreatedAt = now,
                },
            ],
        };
        db.SupportRequests.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return entity.SupportRequestId;
    }

    private Task<int?> FindExistingAsync(int paymentTransactionId, CancellationToken cancellationToken) =>
        db
            .SupportRequests.AsNoTracking()
            .Where(r => r.SourcePaymentTransactionId == paymentTransactionId)
            .Select(r => (int?)r.SupportRequestId)
            .FirstOrDefaultAsync(cancellationToken);
}
