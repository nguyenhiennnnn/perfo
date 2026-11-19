namespace Cinema.Application.Utils;

public enum SeatCoupleViolationKind
{
    WrongSize,
    DifferentRows,
    NotAdjacent,
    OnAisle,
    DifferentSeatType,
    DifferentActiveState,
}

public record CoupleSeatInput(
    int Ref,
    string Row,
    int Number,
    int? SeatTypeId,
    bool IsActive,
    bool IsAisle,
    string? GroupKey
);

public record SeatCoupleViolation(
    string GroupKey,
    SeatCoupleViolationKind Kind,
    IReadOnlyList<int> Refs
);

public static class SeatCoupleRules
{
    public static List<SeatCoupleViolation> Validate(IEnumerable<CoupleSeatInput> seats)
    {
        var violations = new List<SeatCoupleViolation>();

        foreach (var group in seats.Where(s => s.GroupKey is not null).GroupBy(s => s.GroupKey!))
        {
            var members = group
                .OrderBy(s => s.Row.Length)
                .ThenBy(s => s.Row, StringComparer.Ordinal)
                .ThenBy(s => s.Number)
                .ToList();

            if (Check(members) is { } kind)
                violations.Add(new SeatCoupleViolation(group.Key, kind, members.Select(m => m.Ref).ToList()));
        }

        return violations;
    }

    public static List<string> FindDuplicatePositions(IEnumerable<(string Row, int Number)> seats) =>
        seats
            .GroupBy(s => (Row: s.Row.Trim().ToUpperInvariant(), s.Number))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.Row}{g.Key.Number}")
            .ToList();

    private static SeatCoupleViolationKind? Check(List<CoupleSeatInput> members)
    {
        if (members.Count != 2)
            return SeatCoupleViolationKind.WrongSize;

        var (first, second) = (members[0], members[1]);

        if (
            !string.Equals(
                first.Row.Trim(),
                second.Row.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
        )
            return SeatCoupleViolationKind.DifferentRows;

        if (second.Number - first.Number != 1)
            return SeatCoupleViolationKind.NotAdjacent;

        if (first.IsAisle || second.IsAisle)
            return SeatCoupleViolationKind.OnAisle;

        if (first.SeatTypeId != second.SeatTypeId)
            return SeatCoupleViolationKind.DifferentSeatType;

        if (first.IsActive != second.IsActive)
            return SeatCoupleViolationKind.DifferentActiveState;

        return null;
    }
}
