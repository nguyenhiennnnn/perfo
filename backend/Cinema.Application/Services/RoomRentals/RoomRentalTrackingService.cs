using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalTrackingService(
    IAppDbContext db,
    ITokenService tokenService,
    IRoomRentalTrackingAttemptGuard attemptGuard
) : IRoomRentalTrackingService
{
    public async Task<RoomRentalTrackingDto> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken
    )
    {
        await attemptGuard.EnsureNotLockedAsync(cancellationToken);

        var hash = tokenService.HashOpaqueToken(token.Trim());
        var now = DateTime.UtcNow;
        var result = await db
            .RoomRentalRequests.AsNoTracking()
            .Where(r => r.TrackingTokenHash == hash && r.TrackingTokenExpiresAt > now)
            .Select(r => new RoomRentalTrackingDto(
                r.RequestCode,
                r.Status,
                r.FullName,
                r.OrganizationName,
                r.AudienceType,
                r.DesiredStartAt,
                r.FlexibleFromAt,
                r.FlexibleToAt,
                r.AttendeeCount,
                r.Movie != null ? r.Movie.Title : null,
                r.BringsOwnContent,
                r.ContentNote,
                r.ScreenType != null ? r.ScreenType.Name : null,
                r.ComboRequest,
                r.Notes,
                r.CreatedAt,
                r.UpdatedAt,
                r.TrackingTokenExpiresAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
        if (result is not null)
            return result;

        await attemptGuard.RecordFailureAsync(cancellationToken);
        throw new NotFoundException(Messages.NotFound_RoomRentalTracking);
    }
}
