using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.CounterShifts;

public record CounterShiftCashTotals(decimal Collected, decimal Refunded, decimal ChangeGiven)
{
    public decimal Expected(decimal openingCash) => openingCash + Collected - Refunded - ChangeGiven;
}

public static class CounterShiftCash
{
    public static async Task<CounterShiftCashTotals> LoadCounterShiftCashTotalsAsync(
        this IAppDbContext db,
        int counterShiftId,
        CancellationToken cancellationToken
    )
    {
        var rows = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.CounterShiftId == counterShiftId
                && t.Method == PaymentMethod.Cash
                && t.Status == PaymentStatus.Success
            )
            .GroupBy(t => t.Type)
            .Select(g => new
            {
                Type = g.Key,
                Amount = g.Sum(t => t.Amount),
                Received = g.Sum(t => t.AmountReceived ?? t.Amount),
                Change = g.Sum(t => t.ChangeGiven ?? 0m),
            })
            .ToListAsync(cancellationToken);

        var charges = rows.FirstOrDefault(r => r.Type == PaymentTransactionType.Charge);
        var refunds = rows.FirstOrDefault(r => r.Type == PaymentTransactionType.Refund);
        return new CounterShiftCashTotals(
            charges?.Received ?? 0m,
            refunds?.Amount ?? 0m,
            charges?.Change ?? 0m
        );
    }
}
