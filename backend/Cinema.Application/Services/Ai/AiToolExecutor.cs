using System.Globalization;
using System.Text.Json.Nodes;
using Cinema.Application.Caching;
using Cinema.Application.DTOs.Ai;
using Cinema.Application.DTOs.Movies;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.Pricing;
using Cinema.Application.Interfaces.Recommendations;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Ai;

public class AiToolExecutor(
    IAppDbContext db,
    IPaymentLedgerService paymentLedger,
    IPricingService pricingService,
    IMovieRecommendationService recommendations,
    IAppCache cache,
    ILogger<AiToolExecutor> logger
)
{
    public const string SearchShowtimes = "search_showtimes";
    public const string GetSeatAvailability = "get_seat_availability";
    public const string GetMyOrders = "get_my_orders";
    public const string RecommendMovies = "recommend_movies";

    private const int MaxShowtimeResults = 8;
    private const int MaxOrderResults = 5;
    private const int MaxRecommendedMovies = 5;
    private const int MaxSeatLabels = 40;

    public record ToolResult(JsonObject Response, List<AiCardDto> Cards);

    public static IReadOnlyList<AiToolDefinition> GetToolDefinitions(bool isAuthenticated)
    {
        var tools = new List<AiToolDefinition>
        {
            new(
                SearchShowtimes,
                Messages.Ai_Tool_SearchShowtimes_Description,
                [
                    new AiToolParameter(
                        "movie_id",
                        AiToolParameterType.Integer,
                        Messages.Ai_Param_MovieId
                    ),
                    new AiToolParameter(
                        "movie_title",
                        AiToolParameterType.String,
                        Messages.Ai_Param_MovieTitle
                    ),
                    new AiToolParameter("date", AiToolParameterType.String, Messages.Ai_Param_Date),
                ]
            ),
            new(
                GetSeatAvailability,
                Messages.Ai_Tool_GetSeatAvailability_Description,
                [
                    new AiToolParameter(
                        "showtime_id",
                        AiToolParameterType.Integer,
                        Messages.Ai_Param_ShowtimeId,
                        Required: true
                    ),
                ]
            ),
            new(
                RecommendMovies,
                Messages.Ai_Tool_RecommendMovies_Description,
                [
                    new AiToolParameter("date", AiToolParameterType.String, Messages.Ai_Param_Date),
                    new AiToolParameter(
                        "from_time",
                        AiToolParameterType.String,
                        Messages.Ai_Param_FromTime
                    ),
                ]
            ),
        };

        if (isAuthenticated)
        {
            tools.Add(
                new AiToolDefinition(
                    GetMyOrders,
                    Messages.Ai_Tool_GetMyOrders_Description,
                    [
                        new AiToolParameter(
                            "upcoming_only",
                            AiToolParameterType.Boolean,
                            Messages.Ai_Param_UpcomingOnly
                        ),
                        new AiToolParameter(
                            "status",
                            AiToolParameterType.String,
                            Messages.Ai_Param_OrderStatus,
                            AllowedValues: Enum.GetValues<OrderStatus>()
                                .Select(s => s.ToDtoStatus())
                                .ToList()
                        ),
                    ]
                )
            );
        }

        return tools;
    }

    public async Task<ToolResult> ExecuteAsync(
        string name,
        JsonObject? args,
        int? userId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return name switch
            {
                SearchShowtimes => await SearchShowtimesAsync(args, cancellationToken),
                GetSeatAvailability => await GetSeatAvailabilityAsync(args, cancellationToken),
                GetMyOrders => await GetMyOrdersAsync(args, userId, cancellationToken),
                RecommendMovies => await RecommendMoviesAsync(args, userId, cancellationToken),
                _ => Error(string.Format(Messages.Ai_Error_UnknownFunction, name)),
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI tool {Tool} failed.", name);
            return Error(Messages.Ai_Error_QueryFailed);
        }
    }

    private async Task<ToolResult> SearchShowtimesAsync(JsonObject? args, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var movieId = GetInt(args, "movie_id");

        var query = db
            .Showtimes.AsNoTracking()
            .Where(s => s.Status == ShowtimeStatus.Scheduled && s.StartTime >= now);

        if (GetString(args, "date") is { } dateStr)
        {
            if (
                !DateOnly.TryParseExact(
                    dateStr,
                    DateTimePatterns.IsoDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date
                )
            )
                return Error(Messages.Ai_Error_DateFormat);

            var dayStart = BusinessTime.StartOfDayUtc(date);
            var dayEnd = BusinessTime.StartOfNextDayUtc(date);
            query = query.Where(s => s.StartTime >= dayStart && s.StartTime < dayEnd);
        }
        else
        {
            var end = now.AddDays(await db.LoadShowtimeLookaheadDaysAsync(ct));
            query = query.Where(s => s.StartTime <= end);
        }

        if (movieId.HasValue)
            query = query.Where(s => s.MovieId == movieId.Value);
        else
            query = query.ApplySearch(GetString(args, "movie_title"), (q, pattern) =>
                q.Where(s =>
                    EF.Functions.ILike(EF.Functions.Unaccent(s.Movie.Title), EF.Functions.Unaccent(pattern))
                    || EF.Functions.ILike(EF.Functions.Unaccent(s.Movie.OriginalTitle ?? ""), EF.Functions.Unaccent(pattern))));

        var rows = await query
            .OrderBy(s => s.StartTime)
            .Take(MaxShowtimeResults)
            .Select(s => new
            {
                s.ShowtimeId,
                s.MovieId,
                MovieTitle = s.Movie.Title,
                s.Movie.PosterUrl,
                RoomName = s.Room.RoomName,
                ScreenType = s.Room.ScreenType != null ? s.Room.ScreenType.Name : null,
                s.StartTime,
                AvailableSeats = s.ShowtimeSeats.Count(ss =>
                    ss.Status == ShowtimeSeatStatus.Available
                    || (ss.Status == ShowtimeSeatStatus.Held && ss.HeldUntil < now)
                ),
            })
            .ToListAsync(ct);

        var fromPrices = await pricingService.GetFromPricesAsync(
            [.. rows.Select(r => r.ShowtimeId)],
            now,
            ct
        );

        var items = new JsonArray();
        var cards = new List<AiCardDto>();
        foreach (var r in rows)
        {
            var fromPrice = fromPrices.GetValueOrDefault(r.ShowtimeId);
            var item = new JsonObject
            {
                ["showtimeId"] = r.ShowtimeId,
                ["movieId"] = r.MovieId,
                ["movieTitle"] = r.MovieTitle,
                ["room"] = r.RoomName,
                ["screenType"] = r.ScreenType,
                ["startTime"] = BusinessTime
                    .ToLocal(r.StartTime)
                    .ToString(DateTimePatterns.DateHourMinute, CultureInfo.InvariantCulture),
                ["fromPriceVnd"] = fromPrice?.FromPrice,
                ["availableSeats"] = r.AvailableSeats,
            };
            if (fromPrice is { TicketTypes.Count: > 0 })
                item["ticketTypeFromPricesVnd"] = TicketTypePrices(
                    fromPrice.TicketTypes.Select(t => (t.Name, t.FromPrice))
                );
            items.Add(item);
            cards.Add(
                new AiCardDto(
                    "showtime",
                    r.MovieTitle,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Messages.Ai_Card_ShowtimeSubtitle,
                        BusinessTime.ToLocal(r.StartTime),
                        r.RoomName
                    ),
                    fromPrice is null
                        ? string.Format(Messages.Ai_Card_SeatsOnly, r.AvailableSeats)
                        : string.Format(
                            Messages.Ai_Card_PriceAndSeats,
                            fromPrice.FromPrice,
                            r.AvailableSeats
                        ),
                    r.PosterUrl,
                    $"/booking/{r.ShowtimeId}"
                )
            );
        }

        return new ToolResult(
            new JsonObject { ["count"] = items.Count, ["showtimes"] = items },
            cards
        );
    }

    private async Task<ToolResult> RecommendMoviesAsync(
        JsonObject? args,
        int? userId,
        CancellationToken ct
    )
    {
        DateOnly? date = null;
        if (GetString(args, "date") is { } dateStr)
        {
            if (
                !DateOnly.TryParseExact(
                    dateStr,
                    DateTimePatterns.IsoDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate
                )
            )
                return Error(Messages.Ai_Error_DateFormat);
            date = parsedDate;
        }

        TimeOnly? fromTime = null;
        if (GetString(args, "from_time") is { } timeStr)
        {
            if (
                !TimeOnly.TryParseExact(
                    timeStr,
                    DateTimePatterns.HourMinute,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedTime
                )
            )
                return Error(Messages.Ai_Error_TimeFormat);
            fromTime = parsedTime;
        }

        var result = await recommendations.GetForYouAsync(
            userId,
            MaxRecommendedMovies,
            date is null ? null : new RecommendationWindow(date, fromTime),
            ct
        );

        var movies = new JsonArray();
        var cards = new List<AiCardDto>();
        foreach (var item in result.Items.Where(i => i.Showtimes.Count > 0))
        {
            var showtimes = new JsonArray();
            foreach (var s in item.Showtimes.OrderBy(x => x.StartTime).ThenBy(x => x.ShowtimeId))
                showtimes.Add(
                    new JsonObject
                    {
                        ["showtimeId"] = s.ShowtimeId,
                        ["room"] = s.RoomName,
                        ["screenType"] = s.ScreenType,
                        ["startTime"] = BusinessTime
                            .ToLocal(s.StartTime)
                            .ToString(DateTimePatterns.DateHourMinute, CultureInfo.InvariantCulture),
                        ["fromPriceVnd"] = s.FromPrice,
                        ["availableSeats"] = s.AvailableSeats,
                    }
                );

            movies.Add(
                new JsonObject
                {
                    ["movieId"] = item.Movie.MovieId,
                    ["movieTitle"] = item.Movie.Title,
                    ["genres"] = new JsonArray(
                        item.Movie.Genres.Select(g => (JsonNode?)g).ToArray()
                    ),
                    ["durationMinutes"] = item.Movie.DurationMinutes,
                    ["ageRating"] = item.Movie.AgeRating?.Code,
                    ["averageRating"] = item.Movie.AverageRating,
                    ["reason"] = item.Reason,
                    ["showtimes"] = showtimes,
                }
            );

            var best = item.Showtimes[0];
            cards.Add(
                new AiCardDto(
                    "showtime",
                    item.Movie.Title,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Messages.Ai_Card_ShowtimeSubtitle,
                        BusinessTime.ToLocal(best.StartTime),
                        best.RoomName
                    ),
                    best.FromPrice is { } price
                        ? string.Format(Messages.Ai_Card_PriceAndSeats, price, best.AvailableSeats)
                        : string.Format(Messages.Ai_Card_SeatsOnly, best.AvailableSeats),
                    item.Movie.PosterUrl,
                    $"/booking/{best.ShowtimeId}"
                )
            );
        }

        return new ToolResult(
            new JsonObject
            {
                ["personalized"] = result.Personalized,
                ["count"] = movies.Count,
                ["movies"] = movies,
            },
            cards
        );
    }

    private static JsonArray TicketTypePrices(IEnumerable<(string Name, decimal Price)> prices)
    {
        var array = new JsonArray();
        foreach (var (name, price) in prices)
            array.Add(new JsonObject { ["ticketType"] = name, ["priceVnd"] = price });
        return array;
    }

    private async Task<ToolResult> GetSeatAvailabilityAsync(JsonObject? args, CancellationToken ct)
    {
        var showtimeId = GetInt(args, "showtime_id");
        if (showtimeId is null)
            return Error(Messages.Ai_Error_MissingShowtimeId);

        var now = DateTime.UtcNow;
        var showtime = await db
            .Showtimes.AsNoTracking()
            .Where(s => s.ShowtimeId == showtimeId.Value)
            .Select(s => new
            {
                s.ShowtimeId,
                s.Status,
                s.StartTime,
                MovieTitle = s.Movie.Title,
                s.Movie.PosterUrl,
                RoomName = s.Room.RoomName,
            })
            .FirstOrDefaultAsync(ct);

        if (showtime is null)
            return Error(Messages.NotFound_Showtime);
        if (showtime.Status != ShowtimeStatus.Scheduled || showtime.StartTime < now)
            return Error(Messages.Ai_Error_ShowtimeEnded);

        var defaultSeatTypeName = Messages.Ai_SeatTypeDefault;
        var seats = await db
            .ShowtimeSeats.AsNoTracking()
            .WhereSellable()
            .Where(ss => ss.ShowtimeId == showtimeId.Value && ss.Seat.IsActive && !ss.Seat.IsAisle)
            .Select(ss => new
            {
                ss.SeatId,
                ss.Seat.SeatRow,
                ss.Seat.SeatNumber,
                TypeName = ss.Seat.SeatType != null ? ss.Seat.SeatType.Name : defaultSeatTypeName,
                IsFree = ss.Status == ShowtimeSeatStatus.Available
                    || (ss.Status == ShowtimeSeatStatus.Held && ss.HeldUntil < now),
            })
            .ToListAsync(ct);

        var free = seats.Where(x => x.IsFree).ToList();

        var ticketTypes = await cache.GetAsync(CacheKeys.TicketTypes, ct);
        var typeGroups = seats.GroupBy(x => x.TypeName).ToList();
        var quotesPerType = ticketTypes.Count + 1;
        IReadOnlyList<PriceQuoteDto> quotes = [];
        if (typeGroups.Count > 0)
            quotes = await pricingService.QuoteBatchAsync(
                showtime.ShowtimeId,
                [
                    .. typeGroups.SelectMany(g =>
                        new int?[] { null }
                            .Concat(ticketTypes.Select(t => (int?)t.TicketTypeId))
                            .Select(typeId => new SeatPriceRequest(g.First().SeatId, typeId))
                    ),
                ],
                now,
                ct
            );

        var byType = new JsonArray();
        for (var i = 0; i < typeGroups.Count; i++)
        {
            var group = typeGroups[i];
            var typeQuotes = quotes.Skip(i * quotesPerType).Take(quotesPerType).ToList();
            var entry = new JsonObject
            {
                ["seatType"] = group.Key,
                ["priceVnd"] = typeQuotes[0].ListPrice,
                ["total"] = group.Count(),
                ["available"] = group.Count(x => x.IsFree),
            };
            if (ticketTypes.Count > 0)
                entry["ticketTypePricesVnd"] = TicketTypePrices(
                    ticketTypes
                        .Select((t, index) => (t.Name, Quote: typeQuotes[index + 1]))
                        .Where(x => x.Quote.IsEligible)
                        .Select(x => (x.Name, x.Quote.ListPrice))
                );
            byType.Add(entry);
        }

        var byRow = new JsonArray();
        foreach (var g in free.GroupBy(x => x.SeatRow).OrderBy(g => g.Key))
            byRow.Add(new JsonObject { ["row"] = g.Key, ["available"] = g.Count() });

        var labels = new JsonArray();
        foreach (
            var s in free.OrderBy(x => x.SeatRow, StringComparer.Ordinal)
                .ThenBy(x => x.SeatNumber)
                .Take(MaxSeatLabels)
        )
            labels.Add($"{s.SeatRow}{s.SeatNumber}");

        var response = new JsonObject
        {
            ["showtimeId"] = showtime.ShowtimeId,
            ["movieTitle"] = showtime.MovieTitle,
            ["room"] = showtime.RoomName,
            ["startTime"] = BusinessTime
                .ToLocal(showtime.StartTime)
                .ToString(DateTimePatterns.DateHourMinute, CultureInfo.InvariantCulture),
            ["totalSeats"] = seats.Count,
            ["availableSeats"] = free.Count,
            ["bySeatType"] = byType,
            ["availableByRow"] = byRow,
            ["availableSeatLabels"] = labels,
            ["labelsTruncated"] = free.Count > MaxSeatLabels,
        };

        var card = new AiCardDto(
            "showtime",
            showtime.MovieTitle,
            string.Format(
                CultureInfo.InvariantCulture,
                Messages.Ai_Card_ShowtimeSubtitle,
                BusinessTime.ToLocal(showtime.StartTime),
                showtime.RoomName
            ),
            string.Format(Messages.Ai_Card_SeatsLeft, free.Count, seats.Count),
            showtime.PosterUrl,
            $"/booking/{showtime.ShowtimeId}"
        );

        return new ToolResult(response, [card]);
    }

    private async Task<ToolResult> GetMyOrdersAsync(
        JsonObject? args,
        int? userId,
        CancellationToken ct
    )
    {
        if (userId is null)
            return Error(Messages.Ai_Error_LoginRequired);

        var now = DateTime.UtcNow;
        var query = db.Orders.AsNoTracking().Where(o => o.UserId == userId.Value);

        if (
            GetString(args, "status") is { } statusStr
            && Enum.TryParse<OrderStatus>(statusStr, true, out var status)
        )
            query = query.Where(o => o.Status == status);

        if (GetBool(args, "upcoming_only") == true)
            query = query.Where(o =>
                o.Lines.Any(l =>
                    l.LineType == OrderLineType.Ticket
                    && l.Showtime != null
                    && l.Showtime.StartTime >= now
                )
            );

        var orders = await query
            .OrderByDescending(o => o.OrderId)
            .Take(MaxOrderResults)
            .Select(o => new
            {
                o.OrderId,
                o.OrderCode,
                o.Status,
                o.FinalAmount,
                o.CreatedAt,
            })
            .ToListAsync(ct);

        var orderIds = orders.Select(o => o.OrderId).ToList();
        var lines = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                orderIds.Contains(l.OrderId)
                && (l.LineType == OrderLineType.Ticket || l.LineType == OrderLineType.Combo)
            )
            .OrderBy(l => l.OrderLineId)
            .Select(l => new
            {
                l.OrderId,
                l.LineType,
                l.Quantity,
                l.RefundedQuantity,
                SeatRow = l.ShowtimeSeat != null ? l.ShowtimeSeat.Seat.SeatRow : null,
                SeatNumber = l.ShowtimeSeat != null
                    ? (int?)l.ShowtimeSeat.Seat.SeatNumber
                    : null,
                ComboName = l.Combo != null ? l.Combo.Name : l.Description,
                MovieTitle = l.Showtime != null ? l.Showtime.Movie.Title : null,
                PosterUrl = l.Showtime != null ? l.Showtime.Movie.PosterUrl : null,
                RoomName = l.Showtime != null ? l.Showtime.Room.RoomName : null,
                StartTime = l.Showtime != null ? (DateTime?)l.Showtime.StartTime : null,
            })
            .ToListAsync(ct);
        var linesByOrder = lines.ToLookup(l => l.OrderId);

        var items = new JsonArray();
        var cards = new List<AiCardDto>();
        foreach (var o in orders)
        {
            var orderLines = linesByOrder[o.OrderId].ToList();
            var ticketLines = orderLines.Where(l => l.LineType == OrderLineType.Ticket).ToList();
            var showtimeLine = ticketLines.FirstOrDefault(l => l.StartTime is not null);
            var seats = ticketLines
                .Where(l => l.SeatRow is not null && l.SeatNumber is not null)
                .OrderBy(l => l.SeatRow, StringComparer.Ordinal)
                .ThenBy(l => l.SeatNumber)
                .Select(l => $"{l.SeatRow}{l.SeatNumber}")
                .ToList();
            var combos = orderLines
                .Where(l => l.LineType == OrderLineType.Combo)
                .Select(l => new JsonObject
                {
                    ["name"] = l.ComboName,
                    ["quantity"] = l.Quantity,
                })
                .ToList();
            var summary = await paymentLedger.GetSummaryAsync(o.OrderId, ct);
            var statusText = StatusText(o.Status);

            var item = new JsonObject
            {
                ["orderId"] = o.OrderId,
                ["orderCode"] = o.OrderCode,
                ["status"] = statusText,
                ["seats"] = new JsonArray(seats.Select(s => (JsonNode?)s).ToArray()),
                ["ticketCount"] = ticketLines.Sum(l => l.Quantity),
                ["refundedTicketCount"] = ticketLines.Sum(l => l.RefundedQuantity),
                ["combos"] = new JsonArray(combos.Select(c => (JsonNode?)c).ToArray()),
                ["finalAmountVnd"] = o.FinalAmount,
                ["paidAmountVnd"] = summary.CollectedAmount,
                ["refundedAmountVnd"] = summary.RefundedAmount,
                ["pendingRefundAmountVnd"] = summary.PendingRefundAmount,
            };
            if (showtimeLine is not null)
            {
                item["movieTitle"] = showtimeLine.MovieTitle;
                item["room"] = showtimeLine.RoomName;
                item["startTime"] = BusinessTime
                    .ToLocal(showtimeLine.StartTime!.Value)
                    .ToString(DateTimePatterns.DateHourMinute, CultureInfo.InvariantCulture);
            }
            items.Add(item);

            var detail = string.Format(
                Messages.Ai_Card_OrderStatusLine,
                o.OrderCode,
                statusText,
                o.FinalAmount
            );
            if (summary.RefundedAmount > 0m)
                detail += string.Format(
                    Messages.Ai_Card_OrderRefundedSuffix,
                    summary.RefundedAmount
                );

            var comboNames = string.Join(
                ", ",
                orderLines
                    .Where(l => l.LineType == OrderLineType.Combo)
                    .Select(l => $"{l.ComboName} x{l.Quantity}")
            );
            cards.Add(
                new AiCardDto(
                    "booking",
                    showtimeLine?.MovieTitle
                        ?? string.Format(Messages.Ai_Card_OrderFallbackTitle, o.OrderCode),
                    showtimeLine is null
                        ? string.Format(
                            CultureInfo.InvariantCulture,
                            Messages.Ai_Card_OrderItemsSubtitle,
                            BusinessTime.ToLocal(o.CreatedAt),
                            comboNames
                        )
                        : string.Format(
                            CultureInfo.InvariantCulture,
                            Messages.Ai_Card_OrderSubtitle,
                            BusinessTime.ToLocal(showtimeLine.StartTime!.Value),
                            showtimeLine.RoomName,
                            string.Join(", ", seats)
                        ),
                    detail,
                    showtimeLine?.PosterUrl,
                    $"/account/tickets/{o.OrderId}"
                )
            );
        }

        return new ToolResult(
            new JsonObject { ["count"] = items.Count, ["orders"] = items },
            cards
        );
    }

    private static string StatusText(OrderStatus s) =>
        s switch
        {
            OrderStatus.Pending => Messages.Ai_OrderStatus_Pending,
            OrderStatus.Confirmed => Messages.Ai_OrderStatus_Confirmed,
            OrderStatus.Cancelled => Messages.Ai_OrderStatus_Cancelled,
            OrderStatus.Expired => Messages.Ai_OrderStatus_Expired,
            _ => s.ToString(),
        };

    private static ToolResult Error(string message) =>
        new(new JsonObject { ["error"] = message }, []);

    private static string? GetString(JsonObject? args, string key)
    {
        if (args is null || !args.TryGetPropertyValue(key, out var node) || node is null)
            return null;
        try
        {
            var s = node.GetValue<string>();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
        catch
        {
            return node.ToString() is { Length: > 0 } t ? t : null;
        }
    }

    private static int? GetInt(JsonObject? args, string key)
    {
        if (args is null || !args.TryGetPropertyValue(key, out var node) || node is null)
            return null;
        return int.TryParse(
            node.ToString(),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var v
        )
            ? v
            : null;
    }

    private static bool? GetBool(JsonObject? args, string key)
    {
        if (args is null || !args.TryGetPropertyValue(key, out var node) || node is null)
            return null;
        return bool.TryParse(node.ToString(), out var v) ? v : null;
    }
}
