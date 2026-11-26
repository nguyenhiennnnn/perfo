using Cinema.Application.Caching;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Options;
using Cinema.Application.Services.Audit;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalQuoteDeliveryService(
    IAppDbContext db,
    IAppCache cache,
    ITokenService tokenService,
    INotificationService notificationService,
    IAuditLogger auditLogger,
    IOptions<AppUrlOptions> appUrlOptions
) : IRoomRentalQuoteDeliveryService
{
    public async Task<RoomRentalQuoteVersionDto> SendAsync(
        int requestId,
        int version,
        CancellationToken cancellationToken
    )
    {
        var quote = await RoomRentalQuoteStore.FindAsync(db, requestId, version, true, cancellationToken);
        var request = quote.Request!;

        if (!await RoomRentalQuoteStore.IsCurrentAsync(db, requestId, version, cancellationToken))
            throw new BusinessRuleException(Messages.Rule_RoomRentalQuoteSuperseded);

        var now = DateTime.UtcNow;
        if (quote.ValidUntil <= now)
            throw new BusinessRuleException(Messages.Rule_RoomRentalQuoteExpired);
        if (!RoomRentalQuoteStore.QuotableStatuses.Contains(request.Status))
            throw new BusinessRuleException(Messages.Rule_RoomRentalQuoteStatusInvalid);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var quoteBefore = AuditSnapshots.RoomRentalQuoteState(quote);
        var requestBefore = AuditSnapshots.RoomRentalRequestState(request);
        var statusChanged = request.Status != RoomRentalRequestStatus.Quoted;

        var rawToken = tokenService.GenerateOpaqueToken();
        request.TrackingTokenHash = tokenService.HashOpaqueToken(rawToken);
        request.TrackingTokenExpiresAt = now.AddDays(settings.RoomRentalTrackingTokenValidDays);
        request.Status = RoomRentalRequestStatus.Quoted;
        request.UpdatedAt = now;
        quote.SentAt = now;
        quote.SentToEmail = request.ContactEmail;

        auditLogger.Record(
            AuditActions.SendRoomRentalQuote,
            AuditEntityTypes.RoomRentalQuote,
            quote.RoomRentalQuoteId.ToString(),
            quoteBefore,
            AuditSnapshots.RoomRentalQuoteState(quote)
        );
        if (statusChanged)
        {
            auditLogger.Record(
                AuditActions.ChangeRoomRentalRequestStatus,
                AuditEntityTypes.RoomRentalRequest,
                request.RoomRentalRequestId.ToString(),
                requestBefore,
                AuditSnapshots.RoomRentalRequestState(request)
            );
        }

        await db.SaveChangesAsync(cancellationToken);

        notificationService.SendRoomRentalQuote(
            request.ContactEmail,
            new RoomRentalQuoteEmailData(
                request.FullName,
                request.RequestCode,
                quote.VersionNumber,
                quote.StartAt,
                quote.DurationMinutes,
                quote.Total,
                quote.DepositPercent,
                quote.DepositAmount,
                quote.ValidUntil,
                quote.BalanceDueAt,
                RoomRentalTrackingLink.Build(appUrlOptions.Value.FrontendBaseUrl, rawToken),
                request.TrackingTokenExpiresAt
            )
        );

        return RoomRentalQuoteStore.ToDto(quote, request.RequestCode, true);
    }
}
