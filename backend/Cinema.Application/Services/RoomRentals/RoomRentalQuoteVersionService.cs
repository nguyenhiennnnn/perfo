using Cinema.Application.Caching;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalQuoteVersionService(
    IAppDbContext db,
    IAppCache cache,
    IRoomRentalQuoteService pricingService,
    IAuditLogger auditLogger
) : IRoomRentalQuoteVersionService
{
    public async Task<RoomRentalQuoteVersionDto> CreateAsync(
        int requestId,
        UpsertRoomRentalQuoteVersionRequest request,
        CancellationToken cancellationToken
    )
    {
        var rentalRequest =
            await db.RoomRentalRequests.FirstOrDefaultAsync(
                r => r.RoomRentalRequestId == requestId,
                cancellationToken
            ) ?? throw new NotFoundException(Messages.NotFound_RoomRentalRequest);
        if (!RoomRentalQuoteStore.QuotableStatuses.Contains(rentalRequest.Status))
            throw new BusinessRuleException(Messages.Rule_RoomRentalQuoteStatusInvalid);

        var now = DateTime.UtcNow;
        var startUtc = BusinessTime.ToUtc(request.Pricing.StartAt);
        if (startUtc <= now)
            throw new BusinessRuleException(Messages.Rule_RoomRentalQuoteStartPast);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var validUntil = request.ValidUntil is { } validUntilInput
            ? BusinessTime.ToUtc(validUntilInput)
            : now.AddDays(settings.RoomRentalQuoteValidDays);
        if (validUntil <= now || validUntil > startUtc)
            throw new BusinessRuleException(Messages.Rule_RoomRentalQuoteValidUntilInvalid);

        var balanceDueAt = request.BalanceDueAt is { } balanceDueInput
            ? BusinessTime.ToUtc(balanceDueInput)
            : startUtc.AddDays(-settings.RoomRentalBalanceDueDaysBeforeEvent);
        if (request.BalanceDueAt is null && balanceDueAt <= now)
            balanceDueAt = startUtc;
        if (balanceDueAt <= now || balanceDueAt > startUtc)
            throw new BusinessRuleException(Messages.Rule_RoomRentalQuoteBalanceDueInvalid);

        var pricing = await pricingService.CalculateAsync(request.Pricing, cancellationToken);
        var tiers = request.CancellationTiers ?? RoomRentalCancellationPolicy.Defaults(settings);
        var depositPercent = request.DepositPercent ?? settings.RoomRentalDepositPercent;
        var depositAmount = OrderPricing.RoundToDong(pricing.Total * depositPercent / 100m);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var lastVersion =
            await db
                .RoomRentalQuotes.Where(q => q.RoomRentalRequestId == requestId)
                .MaxAsync(q => (int?)q.VersionNumber, cancellationToken) ?? 0;

        var entity = new RoomRentalQuote
        {
            RoomRentalRequestId = requestId,
            VersionNumber = lastVersion + 1,
            RoomId = pricing.RoomId,
            RoomName = pricing.RoomName,
            RoomRentalRateId = pricing.RoomRentalRateId,
            RoomRentalRateName = pricing.RoomRentalRateName,
            StartAt = startUtc,
            DurationMinutes = pricing.DurationMinutes,
            AttendeeCount = pricing.AttendeeCount,
            Subtotal = pricing.Subtotal,
            DiscountTotal = pricing.DiscountTotal,
            Total = pricing.Total,
            DepositPercent = depositPercent,
            DepositAmount = depositAmount,
            ValidUntil = validUntil,
            BalanceDueAt = balanceDueAt,
            CancellationPolicyJson = RoomRentalCancellationPolicy.Serialize(tiers),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = now,
            Lines =
            [
                .. pricing.Lines.Select(
                    (line, index) =>
                        new RoomRentalQuoteLine
                        {
                            SortOrder = index,
                            Kind = line.Kind,
                            Label = line.Label,
                            Quantity = line.Quantity,
                            UnitPrice = line.UnitPrice,
                            Amount = line.Amount,
                            ReferenceId = line.ReferenceId,
                        }
                ),
            ],
        };

        db.RoomRentalQuotes.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.CreateRoomRentalQuoteVersion,
            AuditEntityTypes.RoomRentalQuote,
            entity.RoomRentalQuoteId.ToString(),
            null,
            AuditSnapshots.RoomRentalQuoteState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RoomRentalQuoteStore.ToDto(entity, rentalRequest.RequestCode, true);
    }

    public async Task<List<RoomRentalQuoteVersionListItemDto>> ListAsync(
        int requestId,
        CancellationToken cancellationToken
    )
    {
        if (
            !await db.RoomRentalRequests.AnyAsync(
                r => r.RoomRentalRequestId == requestId,
                cancellationToken
            )
        )
            throw new NotFoundException(Messages.NotFound_RoomRentalRequest);

        var rows = await db
            .RoomRentalQuotes.AsNoTracking()
            .Where(q => q.RoomRentalRequestId == requestId)
            .OrderByDescending(q => q.VersionNumber)
            .Select(q => new
            {
                q.RoomRentalQuoteId,
                q.VersionNumber,
                q.Total,
                q.DepositAmount,
                q.ValidUntil,
                q.SentAt,
                q.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(
                (r, index) =>
                    new RoomRentalQuoteVersionListItemDto(
                        r.RoomRentalQuoteId,
                        r.VersionNumber,
                        index == 0,
                        r.Total,
                        r.DepositAmount,
                        r.ValidUntil,
                        r.SentAt,
                        r.CreatedAt
                    )
            ),
        ];
    }

    public async Task<RoomRentalQuoteVersionDto> GetAsync(
        int requestId,
        int version,
        CancellationToken cancellationToken
    )
    {
        var quote = await RoomRentalQuoteStore.FindAsync(
            db,
            requestId,
            version,
            false,
            cancellationToken
        );
        var isCurrent = await RoomRentalQuoteStore.IsCurrentAsync(
            db,
            requestId,
            version,
            cancellationToken
        );
        return RoomRentalQuoteStore.ToDto(quote, quote.Request!.RequestCode, isCurrent);
    }
}
