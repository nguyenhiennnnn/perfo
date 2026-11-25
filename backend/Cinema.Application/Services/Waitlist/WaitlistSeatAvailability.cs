using Cinema.Application.Utils;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Waitlist;

public static class WaitlistSeatAvailability
{
    public static bool HasFeasibleSelection(
        IReadOnlyList<SeatSlot> seats,
        int seatCount,
        bool requireAdjacent,
        SeatGapRuleMode gapRuleMode
    ) => FindSelection(seats, seatCount, requireAdjacent, gapRuleMode) is not null;

    public static List<int>? FindSelection(
        IReadOnlyList<SeatSlot> seats,
        int seatCount,
        bool requireAdjacent,
        SeatGapRuleMode gapRuleMode
    )
    {
        if (seatCount < 1)
            return null;

        var rows = OrderRows(seats);

        foreach (var row in rows)
            if (FindAdjacentWindow(row, seatCount, gapRuleMode) is { } window)
                return window;

        return requireAdjacent ? null : FindScattered(rows, seatCount, gapRuleMode);
    }

    private static List<List<SeatSlot>> OrderRows(IReadOnlyList<SeatSlot> seats)
    {
        var rows = seats
            .GroupBy(s => s.Row)
            .OrderBy(g => g.Key.Length)
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => g.OrderBy(s => s.Number).ToList())
            .ToList();

        return rows
            .Select((row, index) => (Row: row, Index: index, Penalty: RowPenalty(index, rows.Count)))
            .OrderBy(x => x.Penalty)
            .ThenBy(x => x.Index)
            .Select(x => x.Row)
            .ToList();
    }

    private static double RowPenalty(int index, int rowCount)
    {
        var depth = (index + 1d) / rowCount;
        return Math.Max(Math.Max(0.5 - depth, depth - 0.7), 0);
    }

    private static List<int>? FindAdjacentWindow(
        List<SeatSlot> row,
        int seatCount,
        SeatGapRuleMode gapRuleMode
    )
    {
        var center = (row[0].Number + row[^1].Number) / 2d;
        var candidates = new List<(List<SeatSlot> Window, double Distance)>();

        for (var i = 0; i + seatCount <= row.Count; i++)
        {
            var window = row.GetRange(i, seatCount);
            if (!IsAvailableRun(window))
                continue;

            var ids = window.Select(s => s.ShowtimeSeatId).ToHashSet();
            if (SeatLayoutRules.ValidateSelection(row, ids, gapRuleMode) is not null)
                continue;

            var windowCenter = (window[0].Number + window[^1].Number) / 2d;
            candidates.Add((window, Math.Abs(windowCenter - center)));
        }

        return candidates
            .OrderBy(c => c.Distance)
            .Select(c => c.Window.Select(s => s.ShowtimeSeatId).ToList())
            .FirstOrDefault();
    }

    private static bool IsAvailableRun(List<SeatSlot> window)
    {
        for (var i = 0; i < window.Count; i++)
        {
            if (window[i].Status != ShowtimeSeatStatus.Available)
                return false;
            if (i > 0 && window[i].Number != window[i - 1].Number + 1)
                return false;
        }

        return true;
    }

    private static List<int>? FindScattered(
        List<List<SeatSlot>> rows,
        int seatCount,
        SeatGapRuleMode gapRuleMode
    )
    {
        var remaining = seatCount;
        var result = new List<int>();
        foreach (var row in rows)
        {
            var picked = new HashSet<int>();
            var units = row.Where(s => s.Status == ShowtimeSeatStatus.Available)
                .GroupBy(s => s.CoupleId?.ToString() ?? $"seat-{s.ShowtimeSeatId}")
                .Select(g => g.ToList())
                .Where(unit =>
                    unit[0].CoupleId is null
                    || unit.Count == row.Count(s => s.CoupleId == unit[0].CoupleId)
                )
                .OrderBy(unit => unit[0].Number);

            foreach (var unit in units)
            {
                if (unit.Count > remaining)
                    continue;

                var candidate = new HashSet<int>(picked);
                candidate.UnionWith(unit.Select(s => s.ShowtimeSeatId));
                if (SeatLayoutRules.ValidateSelection(row, candidate, gapRuleMode) is not null)
                    continue;

                picked = candidate;
                remaining -= unit.Count;
                if (remaining == 0)
                    break;
            }

            result.AddRange(picked);
            if (remaining == 0)
                return result;
        }

        return null;
    }
}
