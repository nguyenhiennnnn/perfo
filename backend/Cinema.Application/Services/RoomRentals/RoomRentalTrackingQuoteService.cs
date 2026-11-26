using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalTrackingQuoteService(
    IAppDbContext db,
    ITokenService tokenService,
    IRoomRentalTrackingAttemptGuard attemptGuard,
    IRoomRentalQuotePdfService pdfService,
    IAuditLogger auditLogger
) : IRoomRentalTrackingQuoteService
{
    public async Task<RoomRentalTrackingQuotesDto> ListAsync(
        string token,
        CancellationToken cancellationToken
    )
    {
        var requestId = await ResolveRequestIdAsync(token, cancellationToken);
        var request = await db
            .RoomRentalRequests.AsNoTracking()
            .Where(r => r.RoomRentalRequestId == requestId)
            .Select(r => new { r.RequestCode, r.Status })
            .FirstAsync(cancellationToken);
        var quotes = await db
            .RoomRentalQuotes.AsNoTracking()
            .Include(q => q.Lines)
            .Where(q => q.RoomRentalRequestId == requestId && q.SentAt != null)
            .OrderByDescending(q => q.VersionNumber)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var respondable = request.Status == RoomRentalRequestStatus.Quoted;
        return new RoomRentalTrackingQuotesDto(
            request.RequestCode,
            request.Status,
            [
                .. quotes.Select(
                    (q, index) => ToDto(q, index == 0, respondable && index == 0 && q.ValidUntil > now)
                ),
            ]
        );
    }

    public async Task<ExportFileDto> ExportPdfAsync(
        string token,
        int version,
        CancellationToken cancellationToken
    )
    {
        var requestId = await ResolveRequestIdAsync(token, cancellationToken);
        var sent = await db.RoomRentalQuotes.AnyAsync(
            q =>
                q.RoomRentalRequestId == requestId
                && q.VersionNumber == version
                && q.SentAt != null,
            cancellationToken
        );
        if (!sent)
            throw new NotFoundException(Messages.NotFound_RoomRentalQuote);

        return await pdfService.ExportAsync(requestId, version, cancellationToken);
    }

    public Task<RoomRentalTrackingQuoteDecisionDto> AcceptAsync(
        string token,
        int version,
        CancellationToken cancellationToken
    ) =>
        DecideAsync(
            token,
            version,
            RoomRentalRequestStatus.Accepted,
            AuditActions.AcceptRoomRentalQuote,
            null,
            cancellationToken
        );

    public Task<RoomRentalTrackingQuoteDecisionDto> DeclineAsync(
        string token,
        int version,
        string? reason,
        CancellationToken cancellationToken
    ) =>
        DecideAsync(
            token,
            version,
            RoomRentalRequestStatus.Declined,
            AuditActions.DeclineRoomRentalQuote,
            reason,
            cancellationToken
        );

    private async Task<RoomRentalTrackingQuoteDecisionDto> DecideAsync(
        string token,
        int version,
        RoomRentalRequestStatus target,
        string action,
        string? reason,
        CancellationToken cancellationToken
    )
    {
        var requestId = await ResolveRequestIdAsync(token, cancellationToken);
        var request = await db
            .RoomRentalRequests.AsNoTracking()
            .FirstAsync(r => r.RoomRentalRequestId == requestId, cancellationToken);

        var latest = await db
            .RoomRentalQuotes.AsNoTracking()
            .Where(q => q.RoomRentalRequestId == requestId && q.SentAt != null)
            .OrderByDescending(q => q.VersionNumber)
            .Select(q => new { q.VersionNumber, q.ValidUntil })
            .FirstOrDefaultAsync(cancellationToken);
        var versionSent = await db.RoomRentalQuotes.AnyAsync(
            q =>
                q.RoomRentalRequestId == requestId
                && q.VersionNumber == version
                && q.SentAt != null,
            cancellationToken
        );
        if (latest is null || !versionSent)
            throw new NotFoundException(Messages.NotFound_RoomRentalQuote);

        if (request.Status == target && latest.VersionNumber == version)
            return new RoomRentalTrackingQuoteDecisionDto(
                request.RequestCode,
                request.Status,
                version,
                request.UpdatedAt
            );
        if (request.Status != RoomRentalRequestStatus.Quoted)
            throw new BusinessRuleException(Messages.Rule_RoomRentalQuoteResponseStatusInvalid);
        if (latest.VersionNumber != version)
            throw new BusinessRuleException(Messages.Rule_RoomRentalQuoteNotLatest);

        var now = DateTime.UtcNow;
        if (latest.ValidUntil <= now)
            throw new BusinessRuleException(Messages.Rule_RoomRentalQuoteResponseExpired);

        var note = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var before = AuditSnapshots.RoomRentalRequestState(request);
        var changed = false;
        await db.InTransactionAsync(
            async () =>
            {
                var updated = await db
                    .RoomRentalRequests.Where(r =>
                        r.RoomRentalRequestId == requestId
                        && r.Status == RoomRentalRequestStatus.Quoted
                        && db.RoomRentalQuotes.Any(q =>
                            q.RoomRentalRequestId == requestId
                            && q.VersionNumber == version
                            && q.SentAt != null
                            && q.ValidUntil > now
                        )
                        && !db.RoomRentalQuotes.Any(q =>
                            q.RoomRentalRequestId == requestId
                            && q.VersionNumber > version
                            && q.SentAt != null
                        )
                    )
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(r => r.Status, target)
                                .SetProperty(r => r.StatusNote, r => note ?? r.StatusNote)
                                .SetProperty(r => r.UpdatedAt, now),
                        cancellationToken
                    );
                if (updated == 0)
                    return;

                auditLogger.Record(
                    action,
                    AuditEntityTypes.RoomRentalRequest,
                    requestId.ToString(),
                    before,
                    AuditSnapshots.RoomRentalRequestState(
                        target,
                        note ?? request.StatusNote,
                        now
                    )
                );
                await db.SaveChangesAsync(cancellationToken);
                changed = true;
            },
            cancellationToken
        );
        if (!changed)
            throw new BusinessRuleException(Messages.Rule_RoomRentalQuoteResponseStatusInvalid);

        return new RoomRentalTrackingQuoteDecisionDto(request.RequestCode, target, version, now);
    }

    private async Task<int> ResolveRequestIdAsync(string token, CancellationToken cancellationToken)
    {
        await attemptGuard.EnsureNotLockedAsync(cancellationToken);

        var hash = tokenService.HashOpaqueToken(token.Trim());
        var now = DateTime.UtcNow;
        var requestId = await db
            .RoomRentalRequests.AsNoTracking()
            .Where(r => r.TrackingTokenHash == hash && r.TrackingTokenExpiresAt > now)
            .Select(r => (int?)r.RoomRentalRequestId)
            .FirstOrDefaultAsync(cancellationToken);
        if (requestId is { } found)
            return found;

        await attemptGuard.RecordFailureAsync(cancellationToken);
        throw new NotFoundException(Messages.NotFound_RoomRentalTracking);
    }

    private static RoomRentalTrackingQuoteDto ToDto(
        RoomRentalQuote q,
        bool isLatest,
        bool canRespond
    ) =>
        new(
            q.VersionNumber,
            isLatest,
            canRespond,
            q.RoomName,
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
            q.SentAt!.Value
        );
}
