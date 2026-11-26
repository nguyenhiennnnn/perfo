using Cinema.Application.Caching;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalRequestService(
    IAppDbContext db,
    IAppCache cache,
    IClientContextAccessor clientContext,
    ITokenService tokenService,
    INotificationService notificationService,
    IOptions<AppUrlOptions> appUrlOptions
) : IRoomRentalRequestService
{
    public async Task<RoomRentalRequestReceiptDto> CreatePublicAsync(
        CreateRoomRentalRequestRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.MovieId is { } movieId)
        {
            var movieStatus = await db
                .Movies.AsNoTracking()
                .Where(m => m.MovieId == movieId)
                .Select(m => (MovieStatus?)m.Status)
                .FirstOrDefaultAsync(cancellationToken);
            if (movieStatus is null)
                throw new NotFoundException(Messages.NotFound_Movie);
            if (movieStatus == MovieStatus.Ended)
                throw new BusinessRuleException(Messages.Rule_RoomRentalMovieEnded);
        }

        if (
            request.ScreenTypeId is { } screenTypeId
            && !await db.ScreenTypes.AnyAsync(s => s.ScreenTypeId == screenTypeId, cancellationToken)
        )
            throw new NotFoundException(Messages.NotFound_ScreenType);

        var client = clientContext.Current;
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;
        var rawToken = tokenService.GenerateOpaqueToken();
        var entity = new RoomRentalRequest
        {
            RequestCode = await RoomRentalCodes.GenerateUniqueAsync(db, cancellationToken),
            FullName = request.FullName.Trim(),
            OrganizationName = NullIfBlank(request.OrganizationName),
            AudienceType = request.AudienceType,
            ContactPhone = request.ContactPhone.Trim(),
            ContactEmail = request.ContactEmail.Trim().ToLowerInvariant(),
            DesiredStartAt = BusinessTime.ToUtc(request.DesiredStartAt),
            FlexibleFromAt = request.FlexibleFromAt is { } start ? BusinessTime.ToUtc(start) : null,
            FlexibleToAt = request.FlexibleToAt is { } end ? BusinessTime.ToUtc(end) : null,
            AttendeeCount = request.AttendeeCount,
            MovieId = request.BringsOwnContent ? null : request.MovieId,
            BringsOwnContent = request.BringsOwnContent,
            ContentNote = NullIfBlank(request.ContentNote),
            ScreenTypeId = request.ScreenTypeId,
            ComboRequest = NullIfBlank(request.ComboRequest),
            Notes = NullIfBlank(request.Notes),
            Status = RoomRentalRequestStatus.New,
            TrackingTokenHash = tokenService.HashOpaqueToken(rawToken),
            TrackingTokenExpiresAt = now.AddDays(settings.RoomRentalTrackingTokenValidDays),
            ClientIpAddress = client.IpAddress,
            ClientDeviceId = client.DeviceId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.RoomRentalRequests.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        notificationService.SendRoomRentalRequestReceived(
            entity.ContactEmail,
            new RoomRentalRequestReceivedEmailData(
                entity.FullName,
                entity.RequestCode,
                entity.DesiredStartAt,
                entity.AttendeeCount,
                RoomRentalTrackingLink.Build(appUrlOptions.Value.FrontendBaseUrl, rawToken),
                entity.TrackingTokenExpiresAt
            )
        );

        return new RoomRentalRequestReceiptDto(entity.RequestCode, entity.Status, entity.CreatedAt);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
