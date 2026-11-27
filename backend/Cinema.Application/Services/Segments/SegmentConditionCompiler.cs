using System.Linq.Expressions;
using System.Text.Json;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Segments;

public static class SegmentConditionCompiler
{
    private enum FieldKind
    {
        Number,
        Bool,
        Stage,
        Id,
    }

    private sealed record FieldDef(string Property, FieldKind Kind);

    private static readonly Dictionary<string, FieldDef> Fields = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["activityStage"] = new(nameof(CustomerSegmentProfile.ActivityStage), FieldKind.Stage),
        ["daysSinceLastOrder"] = new(nameof(CustomerSegmentProfile.DaysSinceLastOrder), FieldKind.Number),
        ["lookbackOrderCount"] = new(nameof(CustomerSegmentProfile.LookbackOrderCount), FieldKind.Number),
        ["lookbackSpent"] = new(nameof(CustomerSegmentProfile.LookbackSpent), FieldKind.Number),
        ["totalSpent"] = new(nameof(CustomerSegmentProfile.TotalSpent), FieldKind.Number),
        ["recencyScore"] = new(nameof(CustomerSegmentProfile.RecencyScore), FieldKind.Number),
        ["frequencyScore"] = new(nameof(CustomerSegmentProfile.FrequencyScore), FieldKind.Number),
        ["monetaryScore"] = new(nameof(CustomerSegmentProfile.MonetaryScore), FieldKind.Number),
        ["estimatedLifetimeValue"] = new(nameof(CustomerSegmentProfile.EstimatedLifetimeValue), FieldKind.Number),
        ["expiringPoints"] = new(nameof(CustomerSegmentProfile.ExpiringPoints), FieldKind.Number),
        ["pointsToNextTier"] = new(nameof(CustomerSegmentProfile.PointsToNextTier), FieldKind.Number),
        ["isHighSpender"] = new(nameof(CustomerSegmentProfile.IsHighSpender), FieldKind.Bool),
        ["isHighFrequency"] = new(nameof(CustomerSegmentProfile.IsHighFrequency), FieldKind.Bool),
        ["isNearTierUpgrade"] = new(nameof(CustomerSegmentProfile.IsNearTierUpgrade), FieldKind.Bool),
        ["preferredGenreId"] = new(nameof(CustomerSegmentProfile.PreferredGenreId), FieldKind.Id),
        ["preferredScreenTypeId"] = new(nameof(CustomerSegmentProfile.PreferredScreenTypeId), FieldKind.Id),
        ["nextTierId"] = new(nameof(CustomerSegmentProfile.NextTierId), FieldKind.Id),
    };

    private static readonly string[] NumberOps = ["eq", "neq", "gt", "gte", "lt", "lte"];
    private static readonly string[] BoolOps = ["eq"];
    private static readonly string[] ListOps = ["eq", "neq", "in"];
    private const string NullOp = "isnull";
    private const string NotNullOp = "isnotnull";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize(SegmentConditionNode node) =>
        JsonSerializer.Serialize(node, JsonOptions);

    public static SegmentConditionNode Deserialize(string json) =>
        JsonSerializer.Deserialize<SegmentConditionNode>(json, JsonOptions)!;

    public static SegmentConditionIssue Validate(SegmentConditionNode? root)
    {
        if (root is null || !IsGroup(root))
            return SegmentConditionIssue.Invalid;

        var conditionCount = 0;
        var issue = ValidateNode(root, 1, ref conditionCount);
        if (issue != SegmentConditionIssue.None)
            return issue;
        return conditionCount > SegmentConditionLimits.MaxConditions
            ? SegmentConditionIssue.TooComplex
            : SegmentConditionIssue.None;
    }

    public static Expression<Func<CustomerSegmentProfile, bool>> Compile(SegmentConditionNode root)
    {
        var parameter = Expression.Parameter(typeof(CustomerSegmentProfile), "p");
        return Expression.Lambda<Func<CustomerSegmentProfile, bool>>(
            Build(root, parameter),
            parameter
        );
    }

    private static bool IsGroup(SegmentConditionNode node) => node.Operator is not null;

    private static SegmentConditionIssue ValidateNode(
        SegmentConditionNode node,
        int depth,
        ref int conditionCount
    )
    {
        if (depth > SegmentConditionLimits.MaxDepth)
            return SegmentConditionIssue.TooComplex;

        if (IsGroup(node))
        {
            if (
                node.Field is not null
                || node.Op is not null
                || node.Value is not null
                || node.Values is not null
                || !IsGroupOperator(node.Operator)
                || node.Items is not { Count: > 0 }
            )
                return SegmentConditionIssue.Invalid;

            foreach (var item in node.Items)
            {
                var issue = ValidateNode(item, depth + 1, ref conditionCount);
                if (issue != SegmentConditionIssue.None)
                    return issue;
            }
            return SegmentConditionIssue.None;
        }

        conditionCount++;
        if (conditionCount > SegmentConditionLimits.MaxConditions)
            return SegmentConditionIssue.TooComplex;
        return ValidateCondition(node) ? SegmentConditionIssue.None : SegmentConditionIssue.Invalid;
    }

    private static bool ValidateCondition(SegmentConditionNode node)
    {
        if (
            node.Items is not null
            || node.Field is null
            || node.Op is null
            || !Fields.TryGetValue(node.Field, out var field)
        )
            return false;

        var op = node.Op.ToLowerInvariant();
        var nullable = IsNullable(field);
        if (op is NullOp or NotNullOp)
            return nullable && node.Value is null && node.Values is null;

        if (op == "in")
        {
            return field.Kind is FieldKind.Stage or FieldKind.Id
                && node.Value is null
                && node.Values is { Count: > 0 } values
                && values.Count <= SegmentConditionLimits.MaxListValues
                && values.All(v => TryParseValue(field, v, out _));
        }

        var allowed = field.Kind switch
        {
            FieldKind.Number => NumberOps,
            FieldKind.Bool => BoolOps,
            _ => ListOps,
        };
        return allowed.Contains(op)
            && node.Values is null
            && node.Value is not null
            && TryParseValue(field, node.Value, out _);
    }

    private static bool IsGroupOperator(string? op) =>
        string.Equals(op, "and", StringComparison.OrdinalIgnoreCase)
        || string.Equals(op, "or", StringComparison.OrdinalIgnoreCase);

    private static Type PropertyType(FieldDef field) =>
        typeof(CustomerSegmentProfile).GetProperty(field.Property)!.PropertyType;

    private static bool IsNullable(FieldDef field)
    {
        var type = PropertyType(field);
        return Nullable.GetUnderlyingType(type) is not null;
    }

    private static bool TryParseValue(FieldDef field, string raw, out object? value)
    {
        value = null;
        if (raw.Length is 0 or > SegmentConditionLimits.MaxValueLength)
            return false;

        var type = PropertyType(field);
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        switch (field.Kind)
        {
            case FieldKind.Bool:
                if (!bool.TryParse(raw, out var flag))
                    return false;
                value = flag;
                return true;
            case FieldKind.Stage:
                if (
                    !Enum.GetNames<CustomerActivityStage>()
                        .Contains(raw, StringComparer.OrdinalIgnoreCase)
                )
                    return false;
                value = Enum.Parse<CustomerActivityStage>(raw, true);
                return true;
            case FieldKind.Id:
                if (
                    !int.TryParse(
                        raw,
                        System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var id
                    )
                    || id <= 0
                )
                    return false;
                value = id;
                return true;
            default:
                if (
                    !decimal.TryParse(
                        raw,
                        System.Globalization.NumberStyles.AllowLeadingSign
                            | System.Globalization.NumberStyles.AllowDecimalPoint,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var number
                    )
                )
                    return false;
                if (underlying == typeof(int))
                {
                    if (number != decimal.Truncate(number) || Math.Abs(number) > int.MaxValue)
                        return false;
                    value = (int)number;
                }
                else
                    value = number;
                return true;
        }
    }

    private static Expression Build(SegmentConditionNode node, ParameterExpression parameter)
    {
        if (IsGroup(node))
        {
            var isAnd = string.Equals(node.Operator, "and", StringComparison.OrdinalIgnoreCase);
            return node
                .Items!.Select(i => Build(i, parameter))
                .Aggregate(
                    (left, right) =>
                        isAnd ? Expression.AndAlso(left, right) : Expression.OrElse(left, right)
                );
        }

        var field = Fields[node.Field!];
        var property = Expression.Property(parameter, field.Property);
        var op = node.Op!.ToLowerInvariant();
        if (op == NullOp)
            return Expression.Equal(property, Expression.Constant(null, property.Type));
        if (op == NotNullOp)
            return Expression.NotEqual(property, Expression.Constant(null, property.Type));

        if (op == "in")
            return node
                .Values!.Select(v => Compare(property, field, "eq", v))
                .Aggregate(Expression.OrElse);

        return Compare(property, field, op, node.Value!);
    }

    private static Expression Compare(
        MemberExpression property,
        FieldDef field,
        string op,
        string raw
    )
    {
        TryParseValue(field, raw, out var value);
        var constant = Expression.Constant(value, property.Type);
        return op switch
        {
            "eq" => Expression.Equal(property, constant),
            "neq" => Expression.NotEqual(property, constant),
            "gt" => Expression.GreaterThan(property, constant),
            "gte" => Expression.GreaterThanOrEqual(property, constant),
            "lt" => Expression.LessThan(property, constant),
            _ => Expression.LessThanOrEqual(property, constant),
        };
    }
}
