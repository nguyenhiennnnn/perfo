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
    )
    {
        if (seatCount < 1)
            return false;

        var rows = seats
            .GroupBy(s => s.Row)
            .Select(g => g.OrderBy(s => s.Number).ToList())
            .ToList();

        if (rows.Any(row => HasAdjacentWindow(row, seatCount, gapRuleMode)))
            return true;

        return !requireAdjacent && CanPickScattered(rows, seatCount, gapRuleMode);
    }

    private static bool HasAdjacentWindow(
        List<SeatSlot> row,
        int seatCount,
        SeatGapRuleMode gapRuleMode
    )
    {
        for (var i = 0; i + seatCount <= row.Count; i++)
        {
            var window = row.GetRange(i, seatCount);
            if (!IsAvailableRun(window))
                continue;

            var ids = window.Select(s => s.ShowtimeSeatId).ToHashSet();
            if (SeatLayoutRules.ValidateSelection(row, ids, gapRuleMode) is null)
                return true;
        }

        return false;
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

    private static bool CanPickScattered(
        List<List<SeatSlot>> rows,
        int seatCount,
        SeatGapRuleMode gapRuleMode
    )
    {
        var remaining = seatCount;
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
                    return true;
            }
        }

        return false;
    }
}
