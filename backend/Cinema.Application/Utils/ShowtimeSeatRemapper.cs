namespace Cinema.Application.Utils;

public enum SeatMatchKind
{
    Exact,
    Nearest,
    Manual,
    Unmapped,
}

public record RemapSourceSeat(
    int Id,
    string Row,
    int Number,
    int? SeatTypeId,
    bool IsCouple,
    Guid? CoupleId,
    bool Required
);

public record RemapTargetSeat(
    int Id,
    string Row,
    int Number,
    int? SeatTypeId,
    bool IsCouple,
    Guid? CoupleId
);

public record RemapResult(int SourceId, int? TargetId, SeatMatchKind Kind);

public static class ShowtimeSeatRemapper
{
    private const int RowWeight = 3;
    private const int TypePenalty = 6;
    private const int CouplePenalty = 100;

    public static List<RemapResult> Map(
        IReadOnlyList<RemapSourceSeat> sources,
        IReadOnlyList<RemapTargetSeat> targets,
        IReadOnlyDictionary<int, int> overrides
    )
    {
        var results = new Dictionary<int, RemapResult>();
        var used = new HashSet<int>();
        var targetById = targets.ToDictionary(t => t.Id);

        foreach (var source in sources)
        {
            if (
                overrides.TryGetValue(source.Id, out var targetId)
                && targetById.ContainsKey(targetId)
                && used.Add(targetId)
            )
                results[source.Id] = new RemapResult(source.Id, targetId, SeatMatchKind.Manual);
        }

        var byPosition = targets
            .GroupBy(t => PositionKey(t.Row, t.Number))
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var source in sources.Where(s => !results.ContainsKey(s.Id)))
        {
            if (
                byPosition.TryGetValue(PositionKey(source.Row, source.Number), out var target)
                && target.SeatTypeId == source.SeatTypeId
                && used.Add(target.Id)
            )
                results[source.Id] = new RemapResult(source.Id, target.Id, SeatMatchKind.Exact);
        }

        MapCouplesToNearest(sources, targets, results, used);
        MapSinglesToNearest(sources, targets, results, used);

        return
        [
            .. sources.Select(s =>
                results.GetValueOrDefault(s.Id) ?? new RemapResult(s.Id, null, SeatMatchKind.Unmapped)
            ),
        ];
    }

    private static void MapCouplesToNearest(
        IReadOnlyList<RemapSourceSeat> sources,
        IReadOnlyList<RemapTargetSeat> targets,
        Dictionary<int, RemapResult> results,
        HashSet<int> used
    )
    {
        var pendingGroups = sources
            .Where(s => s.Required && s.IsCouple && s.CoupleId is not null && !results.ContainsKey(s.Id))
            .GroupBy(s => s.CoupleId!.Value)
            .Select(g => g.OrderBy(s => RowIndex(s.Row)).ThenBy(s => s.Number).ToList())
            .Where(g => g.All(s => !results.ContainsKey(s.Id)))
            .OrderBy(g => RowIndex(g[0].Row))
            .ThenBy(g => g[0].Number)
            .ToList();

        var targetGroups = targets
            .Where(t => t.IsCouple && t.CoupleId is not null)
            .GroupBy(t => t.CoupleId!.Value)
            .Select(g => g.OrderBy(t => RowIndex(t.Row)).ThenBy(t => t.Number).ToList())
            .ToList();

        foreach (var group in pendingGroups)
        {
            List<RemapTargetSeat>? best = null;
            var bestScore = int.MaxValue;
            foreach (var candidate in targetGroups)
            {
                if (candidate.Count != group.Count || candidate.Any(t => used.Contains(t.Id)))
                    continue;

                var score = 0;
                for (var i = 0; i < group.Count; i++)
                    score += Score(group[i], candidate[i]);

                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best is null)
                continue;

            for (var i = 0; i < group.Count; i++)
            {
                used.Add(best[i].Id);
                results[group[i].Id] = new RemapResult(group[i].Id, best[i].Id, SeatMatchKind.Nearest);
            }
        }
    }

    private static void MapSinglesToNearest(
        IReadOnlyList<RemapSourceSeat> sources,
        IReadOnlyList<RemapTargetSeat> targets,
        Dictionary<int, RemapResult> results,
        HashSet<int> used
    )
    {
        var pending = sources
            .Where(s => s.Required && !results.ContainsKey(s.Id))
            .OrderBy(s => RowIndex(s.Row))
            .ThenBy(s => s.Number)
            .ThenBy(s => s.Id);

        foreach (var source in pending)
        {
            RemapTargetSeat? best = null;
            var bestScore = int.MaxValue;
            foreach (var target in targets)
            {
                if (used.Contains(target.Id))
                    continue;

                var score = Score(source, target);
                if (score < bestScore || (score == bestScore && best is not null && target.Id < best.Id))
                {
                    bestScore = score;
                    best = target;
                }
            }

            if (best is null)
                break;

            used.Add(best.Id);
            results[source.Id] = new RemapResult(source.Id, best.Id, SeatMatchKind.Nearest);
        }
    }

    private static int Score(RemapSourceSeat source, RemapTargetSeat target) =>
        Math.Abs(RowIndex(source.Row) - RowIndex(target.Row)) * RowWeight
        + Math.Abs(source.Number - target.Number)
        + (source.SeatTypeId == target.SeatTypeId ? 0 : TypePenalty)
        + (source.IsCouple == target.IsCouple ? 0 : CouplePenalty);

    private static string PositionKey(string row, int number) =>
        $"{row.Trim().ToUpperInvariant()}|{number}";

    public static int RowIndex(string row)
    {
        var index = 0;
        foreach (var c in row.Trim().ToUpperInvariant())
        {
            if (c is < 'A' or > 'Z')
                return int.TryParse(row, out var numeric) ? numeric : 0;
            index = index * 26 + (c - 'A' + 1);
        }

        return index;
    }
}
