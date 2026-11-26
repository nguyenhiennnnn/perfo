using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Bookings;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Services.Showtimes;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalShowtimeService(
    IAppDbContext db,
    ISeatBlockService seatBlockService,
    ISalesStatsService salesStats,
    IAuditLogger auditLogger,
    ILogger<RoomRentalShowtimeService> logger
) : IRoomRentalShowtimeService
{
    public async Task<RoomRentalShowtimeDto> EnsureForRequestAsync(
        int requestId,
        CancellationToken cancellationToken
    )
    {
        var request =
            await db
                .RoomRentalRequests.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoomRentalRequestId == requestId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_RoomRentalRequest);

        var existing = await FindExistingAsync(requestId, cancellationToken);
        if (existing is not null)
            return existing;

        if (
            request.Status
            is not (RoomRentalRequestStatus.DepositPaid or RoomRentalRequestStatus.FullyPaid)
        )
            throw new BusinessRuleException(Messages.Rule_RoomRentalShowtimeStatusInvalid);

        var quote =
            await db
                .RoomRentalQuotes.AsNoTracking()
                .Where(q => q.RoomRentalRequestId == requestId && q.SentAt != null)
                .OrderByDescending(q => q.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_RoomRentalQuote);

        var room =
            await db
                .CinemaRooms.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoomId == quote.RoomId, cancellationToken)
            ?? throw new BusinessRuleException(Messages.Invalid_RoomIdNotFound);
        if (!room.IsActive)
            throw new BusinessRuleException(Messages.Rule_RoomInactive);

        ShowtimeRules.EnsureStartsInFuture(quote.StartAt);

        var bufferMinutes = (
            await db.LoadCinemaSettingsOrDefaultAsync(cancellationToken)
        ).RoomBufferMinutes;
        var startTime = quote.StartAt;
        var endTime = startTime.AddMinutes(quote.DurationMinutes + bufferMinutes);

        var showtime = new Showtime
        {
            RoomId = room.RoomId,
            StartTime = startTime,
            EndTime = endTime,
            BasePrice = 0m,
            Status = ShowtimeStatus.Scheduled,
            Kind = ShowtimeKind.Private,
            RoomRentalRequestId = requestId,
            ContentTitle = ResolveContentTitle(request),
            CreatedAt = DateTime.UtcNow,
        };

        var seatCount = 0;
        await db.InTransactionAsync(
            async () =>
            {
                await db.EnsureNoOverlapAsync(
                    room.RoomId,
                    startTime,
                    endTime,
                    excludeShowtimeId: null,
                    cancellationToken
                );

                showtime.MovieId = await ResolveMovieIdAsync(
                    request.MovieId,
                    quote.DurationMinutes,
                    cancellationToken
                );
                seatCount = await db.GenerateShowtimeSeatsAsync(
                    showtime,
                    room.RoomId,
                    0m,
                    cancellationToken
                );
                if (seatCount == 0)
                    throw new BusinessRuleException(Messages.Rule_RoomHasNoSeatLayout);

                db.Showtimes.Add(showtime);
                await db.SaveChangesAsync(cancellationToken);

                var reason = string.Format(
                    Messages.RoomRental_ShowtimeSeatBlockReason,
                    request.RequestCode
                );
                var seatIds = showtime.ShowtimeSeats.Select(ss => ss.ShowtimeSeatId).ToList();
                foreach (var chunk in seatIds.Chunk(SeatBlockLimits.MaxSeatsPerRequest))
                    await seatBlockService.BlockSeatsAsync(
                        showtime.ShowtimeId,
                        chunk,
                        SeatBlockType.RoomRental,
                        reason,
                        null,
                        cancellationToken
                    );

                auditLogger.Record(
                    AuditActions.CreateRoomRentalShowtime,
                    AuditEntityTypes.Showtime,
                    showtime.ShowtimeId.ToString(),
                    null,
                    new
                    {
                        requestId,
                        showtime.RoomId,
                        showtime.StartTime,
                        showtime.EndTime,
                        kind = ShowtimeKind.Private,
                        blockedSeats = seatCount,
                    }
                );
                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );

        await salesStats.RefreshShowtimesAsync([showtime.ShowtimeId], cancellationToken);

        logger.LogInformation(
            "RoomRentalShowtimeCreated RequestId {RequestId} ShowtimeId {ShowtimeId} RoomId {RoomId} BlockedSeats {Count}",
            requestId,
            showtime.ShowtimeId,
            showtime.RoomId,
            seatCount
        );

        return new RoomRentalShowtimeDto(
            showtime.ShowtimeId,
            showtime.RoomId,
            showtime.StartTime,
            showtime.EndTime,
            showtime.ContentTitle,
            seatCount
        );
    }

    private async Task<RoomRentalShowtimeDto?> FindExistingAsync(
        int requestId,
        CancellationToken cancellationToken
    )
    {
        var found = await db
            .Showtimes.AsNoTracking()
            .Where(s => s.RoomRentalRequestId == requestId)
            .Select(s => new
            {
                s.ShowtimeId,
                s.RoomId,
                s.StartTime,
                s.EndTime,
                s.ContentTitle,
                Blocked = s.ShowtimeSeats.Count(ss => ss.Status == ShowtimeSeatStatus.Blocked),
            })
            .FirstOrDefaultAsync(cancellationToken);
        return found is null
            ? null
            : new RoomRentalShowtimeDto(
                found.ShowtimeId,
                found.RoomId,
                found.StartTime,
                found.EndTime,
                found.ContentTitle,
                found.Blocked
            );
    }

    private static string? ResolveContentTitle(RoomRentalRequest request)
    {
        if (!request.BringsOwnContent || string.IsNullOrWhiteSpace(request.ContentNote))
            return null;

        var title = request.ContentNote.Trim();
        return title.Length <= FieldLengths.MovieTitle
            ? title
            : title[..FieldLengths.MovieTitle];
    }

    private async Task<int> ResolveMovieIdAsync(
        int? requestedMovieId,
        int durationMinutes,
        CancellationToken cancellationToken
    )
    {
        if (
            requestedMovieId is { } movieId
            && await db.Movies.AnyAsync(m => m.MovieId == movieId, cancellationToken)
        )
            return movieId;

        var title = Messages.RoomRental_CustomContentMovieTitle;
        var placeholder = await db
            .Movies.Where(m => m.Title == title && m.Status == MovieStatus.Ended)
            .Select(m => (int?)m.MovieId)
            .FirstOrDefaultAsync(cancellationToken);
        if (placeholder is { } placeholderId)
            return placeholderId;

        var movie = new Movie
        {
            Title = title,
            DurationMinutes = Math.Max(1, durationMinutes),
            Status = MovieStatus.Ended,
            CreatedAt = DateTime.UtcNow,
        };
        db.Movies.Add(movie);
        await db.SaveChangesAsync(cancellationToken);
        return movie.MovieId;
    }
}
