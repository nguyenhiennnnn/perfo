using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Interfaces.Showtimes;
using Cinema.Application.Utils;
using Cinema.Application.Validators.Pricing;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Showtimes;

public class ShowtimeEventService(
    IAppDbContext db,
    ISalesStatsService salesStats,
    IAuditLogger auditLogger
) : IShowtimeEventService
{
    public async Task<ShowtimeEventDto> GetAsync(
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        var setting =
            await db
                .ShowtimeEventSettings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ShowtimeId == showtimeId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_ShowtimeEvent);
        return ToDto(setting);
    }

    public async Task<ShowtimeEventDto> UpsertAsync(
        int showtimeId,
        UpsertShowtimeEventRequest request,
        CancellationToken cancellationToken
    )
    {
        var showtime = await LoadEditableAsync(showtimeId, cancellationToken);
        if (showtime.Kind == ShowtimeKind.Private)
            throw new BusinessRuleException(Messages.Rule_ShowtimeEventKindInvalid);

        PricingValidation.TryParseEnum<PricingAdjustmentMode>(request.AdjustmentMode, out var mode);
        var value =
            mode == PricingAdjustmentMode.Amount
                ? OrderPricing.RoundToDong(request.AdjustmentValue)
                : decimal.Round(request.AdjustmentValue, 4);

        var becomesEvent = showtime.Kind != ShowtimeKind.Event;
        if (becomesEvent)
            await EnsureNoActiveOrdersAsync(showtimeId, cancellationToken);

        var now = DateTime.UtcNow;
        var setting = showtime.EventSetting;
        var before = setting is null ? null : ToDto(setting);
        if (setting is null)
        {
            setting = new ShowtimeEventSetting { ShowtimeId = showtimeId, CreatedAt = now };
            db.ShowtimeEventSettings.Add(setting);
        }
        else
            setting.UpdatedAt = now;

        setting.AdjustmentMode = mode;
        setting.AdjustmentValue = value;
        showtime.Kind = ShowtimeKind.Event;

        var after = ToDto(setting);
        auditLogger.Record(
            becomesEvent ? AuditActions.Create : AuditActions.Update,
            AuditEntityTypes.ShowtimeEvent,
            showtimeId.ToString(),
            before,
            after
        );
        await db.SaveChangesAsync(cancellationToken);

        if (becomesEvent)
            await salesStats.RefreshShowtimesAsync([showtimeId], cancellationToken);

        return after;
    }

    public async Task<int> RemoveAsync(int showtimeId, CancellationToken cancellationToken)
    {
        var showtime = await LoadEditableAsync(showtimeId, cancellationToken);
        if (showtime.Kind != ShowtimeKind.Event)
            throw new NotFoundException(Messages.NotFound_ShowtimeEvent);

        await EnsureNoActiveOrdersAsync(showtimeId, cancellationToken);

        var before = showtime.EventSetting is null ? null : ToDto(showtime.EventSetting);
        if (showtime.EventSetting is not null)
            db.ShowtimeEventSettings.Remove(showtime.EventSetting);
        showtime.Kind = ShowtimeKind.Regular;

        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.ShowtimeEvent,
            showtimeId.ToString(),
            before,
            null
        );
        await db.SaveChangesAsync(cancellationToken);
        await salesStats.RefreshShowtimesAsync([showtimeId], cancellationToken);

        return showtimeId;
    }

    private async Task<Showtime> LoadEditableAsync(
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        var showtime =
            await db
                .Showtimes.Include(s => s.EventSetting)
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Showtime);

        if (showtime.Status != ShowtimeStatus.Scheduled || showtime.StartTime <= DateTime.UtcNow)
            throw new BusinessRuleException(Messages.Rule_ShowtimeEventStatusInvalid);

        return showtime;
    }

    private async Task EnsureNoActiveOrdersAsync(
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        var hasOrders = await db
            .OrderLines.AsNoTracking()
            .AnyAsync(
                l =>
                    l.ShowtimeId == showtimeId
                    && (l.Order.Status == OrderStatus.Pending || l.Order.Status == OrderStatus.Confirmed),
                cancellationToken
            );
        if (hasOrders)
            throw new BusinessRuleException(Messages.Rule_ShowtimeEventHasOrders);
    }

    private static ShowtimeEventDto ToDto(ShowtimeEventSetting setting) =>
        new(setting.ShowtimeId, setting.AdjustmentMode.ToString(), setting.AdjustmentValue);
}
