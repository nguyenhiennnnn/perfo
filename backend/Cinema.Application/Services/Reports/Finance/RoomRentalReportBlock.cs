using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class RoomRentalReportBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "room-rental";
    public string Title => Messages.Report_Finance_RoomRental_Title;
    public int Order => 65;

    private sealed record Row(
        string Code,
        string Customer,
        string Room,
        string Showtime,
        int Attendees,
        decimal Quoted,
        decimal Deposit,
        decimal Combo
    );

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = await WindowAsync(period, cancellationToken);
        return await PrivateShowtimes(fromUtc, toUtc).AnyAsync(cancellationToken);
    }

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var (fromUtc, toUtc) = await WindowAsync(period, cancellationToken);

        var showtimes = await PrivateShowtimes(fromUtc, toUtc)
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.ShowtimeId)
            .Select(s => new
            {
                s.ShowtimeId,
                s.StartTime,
                RoomName = s.Room.RoomName,
                RequestId = s.RoomRentalRequestId!.Value,
                s.RoomRentalRequest!.RequestCode,
                s.RoomRentalRequest.FullName,
                s.RoomRentalRequest.OrganizationName,
            })
            .ToListAsync(cancellationToken);

        var requestIds = showtimes.Select(s => s.RequestId).Distinct().ToList();
        var showtimeIds = showtimes.Select(s => s.ShowtimeId).ToList();

        var quotes = (
            await db
                .RoomRentalQuotes.AsNoTracking()
                .Where(q => requestIds.Contains(q.RoomRentalRequestId) && q.SentAt != null)
                .Select(q => new
                {
                    q.RoomRentalRequestId,
                    q.VersionNumber,
                    q.AttendeeCount,
                    q.Total,
                    q.DepositAmount,
                })
                .ToListAsync(cancellationToken)
        )
            .GroupBy(q => q.RoomRentalRequestId)
            .ToDictionary(g => g.Key, g => g.MaxBy(q => q.VersionNumber)!);

        var combos = await db
            .ShowtimeDailyStats.AsNoTracking()
            .Where(d => showtimeIds.Contains(d.ShowtimeId))
            .ToDictionaryAsync(d => d.ShowtimeId, d => d.ComboRevenue, cancellationToken);

        var rows = showtimes
            .Select(s =>
            {
                quotes.TryGetValue(s.RequestId, out var quote);
                return new Row(
                    s.RequestCode,
                    string.IsNullOrWhiteSpace(s.OrganizationName) ? s.FullName : s.OrganizationName,
                    s.RoomName,
                    BusinessTime.ToLocal(s.StartTime).ToString(DateTimePatterns.DateHourMinute),
                    quote?.AttendeeCount ?? 0,
                    quote?.Total ?? 0m,
                    quote?.DepositAmount ?? 0m,
                    combos.GetValueOrDefault(s.ShowtimeId)
                );
            })
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("code", Messages.Report_Finance_Col_RentalCode, ReportColumnType.Text),
            new("customer", Messages.Report_Finance_Col_RentalCustomer, ReportColumnType.Text),
            new("room", Messages.Report_Finance_Col_RentalRoom, ReportColumnType.Text),
            new("showtime", Messages.Report_Finance_Col_Showtime, ReportColumnType.Text),
            new("attendees", Messages.Report_Finance_Col_RentalAttendees, ReportColumnType.Number),
            new("quoted", Messages.Report_Finance_Col_RentalQuoted, ReportColumnType.Money),
            new("deposit", Messages.Report_Finance_Col_RentalDeposit, ReportColumnType.Money),
            new("combo", Messages.Report_Finance_Col_RentalCombo, ReportColumnType.Money),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows.Select(r => ToRow(r.Code, r.Customer, r.Room, r.Showtime, r)).ToList(),
            ToRow(
                Messages.Export_Total,
                null,
                null,
                null,
                new Row(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    rows.Sum(r => r.Attendees),
                    rows.Sum(r => r.Quoted),
                    rows.Sum(r => r.Deposit),
                    rows.Sum(r => r.Combo)
                )
            ),
            [Messages.Report_Finance_RoomRental_Note]
        );
    }

    private IQueryable<Showtime> PrivateShowtimes(
        DateTime fromUtc,
        DateTime toUtc
    ) =>
        db.Showtimes.AsNoTracking()
            .Where(s =>
                s.Kind == ShowtimeKind.Private
                && s.RoomRentalRequestId != null
                && s.Status != ShowtimeStatus.Cancelled
                && s.StartTime >= fromUtc
                && s.StartTime < toUtc
            );

    private async Task<(DateTime FromUtc, DateTime ToUtc)> WindowAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        return (
            BusinessDay.StartUtc(period.From, settings.OpeningTime, settings.ClosingTime),
            BusinessDay.EndUtc(period.To, settings.OpeningTime, settings.ClosingTime)
        );
    }

    private static IReadOnlyDictionary<string, object?> ToRow(
        string? code,
        string? customer,
        string? room,
        string? showtime,
        Row r
    ) =>
        new Dictionary<string, object?>
        {
            ["code"] = code,
            ["customer"] = customer,
            ["room"] = room,
            ["showtime"] = showtime,
            ["attendees"] = r.Attendees,
            ["quoted"] = r.Quoted,
            ["deposit"] = r.Deposit,
            ["combo"] = r.Combo,
        };
}
