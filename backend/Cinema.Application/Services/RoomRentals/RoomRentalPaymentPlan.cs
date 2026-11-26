using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.RoomRentals;

internal static class RoomRentalPaymentPlan
{
    public static decimal AmountFor(RoomRentalQuote quote, RoomRentalPaymentStage stage) =>
        stage == RoomRentalPaymentStage.Deposit
            ? quote.DepositAmount
            : quote.Total - quote.DepositAmount;

    public static DateTime? DueAt(RoomRentalQuote quote, RoomRentalPaymentStage stage) =>
        stage == RoomRentalPaymentStage.Balance ? quote.BalanceDueAt : null;

    public static DateTime ExpiresAt(RoomRentalQuote quote, RoomRentalPaymentStage stage) =>
        stage == RoomRentalPaymentStage.Balance ? quote.BalanceDueAt : quote.StartAt;

    public static RoomRentalRequestStatus RequiredStatus(RoomRentalPaymentStage stage) =>
        stage == RoomRentalPaymentStage.Deposit
            ? RoomRentalRequestStatus.Accepted
            : RoomRentalRequestStatus.DepositPaid;

    public static async Task<RoomRentalQuote> LoadLatestSentQuoteAsync(
        IAppDbContext db,
        int requestId,
        CancellationToken cancellationToken
    ) =>
        await db
            .RoomRentalQuotes.AsNoTracking()
            .Where(q => q.RoomRentalRequestId == requestId && q.SentAt != null)
            .OrderByDescending(q => q.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_RoomRentalQuote);
}
