using Cinema.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Payments;

public static class PaymentLocks
{
    public static Task<int> LockOrderAsync(
        this IAppDbContext db,
        int orderId,
        CancellationToken cancellationToken
    ) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Orders\" WHERE \"OrderId\" = {orderId} FOR UPDATE",
            cancellationToken
        );

    public static Task<int> LockTransactionAsync(
        this IAppDbContext db,
        int paymentTransactionId,
        CancellationToken cancellationToken
    ) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"PaymentTransactions\" WHERE \"PaymentTransactionId\" = {paymentTransactionId} FOR UPDATE",
            cancellationToken
        );
}
