namespace Cinema.Application.Services.Segments;

public sealed class SegmentConditionNode
{
    public string? Operator { get; init; }
    public List<SegmentConditionNode>? Items { get; init; }
    public string? Field { get; init; }
    public string? Op { get; init; }
    public string? Value { get; init; }
    public List<string>? Values { get; init; }
}

public static class SegmentConditionLimits
{
    public const int MaxDepth = 3;
    public const int MaxConditions = 20;
    public const int MaxListValues = 20;
    public const int MaxValueLength = 50;
}

public enum SegmentConditionIssue
{
    None,
    Invalid,
    TooComplex,
}
