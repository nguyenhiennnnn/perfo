using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.RoomRentals;

public static class RoomRentalQuoteStore
{
    public static readonly IReadOnlyList<RoomRentalRequestStatus> QuotableStatuses =
    [
        RoomRentalRequestStatus.New,
        RoomRentalRequestStatus.Processing,
        RoomRentalRequestStatus.Quoted,
        RoomRentalRequestStatus.QuoteExpired,
    ];

    public static async Task<RoomRentalQuote> FindAsync(
        IAppDbContext db,
        int requestId,
        int version,
        bool track,
        CancellationToken cancellationToken
    )
    {
        var query = db
            .RoomRentalQuotes.Include(q => q.Lines)
            .Include(q => q.Request)
            .Where(q => q.RoomRentalRequestId == requestId && q.VersionNumber == version);
        if (!track)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_RoomRentalQuote);
    }

    public static Task<bool> IsCurrentAsync(
        IAppDbContext db,
        int requestId,
        int version,
        CancellationToken cancellationToken
    ) =>
        db.RoomRentalQuotes.AllAsync(
            q => q.RoomRentalRequestId != requestId || q.VersionNumber <= version,
            cancellationToken
        );

    public static RoomRentalQuoteVersionDto ToDto(
        RoomRentalQuote q,
        string requestCode,
        bool isCurrent
    ) =>
        new(
            q.RoomRentalQuoteId,
            q.RoomRentalRequestId,
            requestCode,
            q.VersionNumber,
            isCurrent,
            q.RoomId,
            q.RoomName,
            q.RoomRentalRateId,
            q.RoomRentalRateName,
            q.StartAt,
            q.DurationMinutes,
            q.AttendeeCount,
            [
                .. q.Lines.OrderBy(l => l.SortOrder)
                    .Select(l => new RoomRentalQuoteLineDto(
                        l.Kind,
                        l.Label,
                        l.Quantity,
                        l.UnitPrice,
                        l.Amount,
                        l.ReferenceId
                    )),
            ],
            q.Subtotal,
            q.DiscountTotal,
            q.Total,
            q.DepositPercent,
            q.DepositAmount,
            q.Total - q.DepositAmount,
            q.ValidUntil,
            q.BalanceDueAt,
            RoomRentalCancellationPolicy.Parse(q.CancellationPolicyJson),
            q.Note,
            q.SentAt,
            q.SentToEmail,
            q.CreatedAt
        );
}
