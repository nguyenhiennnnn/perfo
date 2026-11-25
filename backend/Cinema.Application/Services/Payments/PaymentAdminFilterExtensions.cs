using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Cinema.Application.Interfaces.Payments;


namespace Cinema.Application.Services.Payments;

internal static class PaymentAdminFilterExtensions
{
    public static IQueryable<PaymentTransaction> ApplyAdminFilter(
        this IQueryable<PaymentTransaction> query,
        AdminPaymentListFilter filter
    )
    {
        if (EnumConverters.TryParseEnum<PaymentTransactionType>(filter.Type, out var type))
            query = query.Where(t => t.Type == type);
        if (EnumConverters.TryParseEnum<PaymentStatus>(filter.Status, out var status))
            query = query.Where(t => t.Status == status);
        if (EnumConverters.TryParseEnum<PaymentMethod>(filter.PaymentMethod, out var method))
            query = query.Where(t => t.Method == method);
        if (filter.NeedsRefund == true)
            query = query.Where(t =>
                t.NeedsManualRefund
                || (
                    t.Type == PaymentTransactionType.Refund
                    && t.Method == PaymentMethod.Cash
                    && t.Status == PaymentStatus.Pending
                )
            );
        if (!string.IsNullOrWhiteSpace(filter.OrderCode))
        {
            var orderPattern = SearchHelper.ILikePattern(filter.OrderCode.Trim());
            query = query.Where(t => EF.Functions.ILike(t.Order.OrderCode, orderPattern));
        }
        if (filter.FromDate is not null)
            query = query.Where(t =>
                t.CreatedAt >= DateTimeUtils.GetStartOfDay(filter.FromDate.Value)
            );
        if (filter.ToDate is not null)
            query = query.Where(t =>
                t.CreatedAt < DateTimeUtils.GetStartOfNextDay(filter.ToDate.Value)
            );
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) =>
                q.Where(t =>
                    EF.Functions.ILike(t.TransactionCode, pattern)
                    || (
                        t.GatewayTransactionId != null
                        && EF.Functions.ILike(t.GatewayTransactionId, pattern)
                    )
                    || EF.Functions.ILike(t.Order.OrderCode, pattern)
                )
        );

        return query;
    }
}
