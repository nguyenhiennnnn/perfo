using System.Text.Json;
using Cinema.Application.Caching;
using Cinema.Application.DTOs.RoomRentals;

namespace Cinema.Application.Services.RoomRentals;

public static class RoomRentalCancellationPolicy
{
    public const int MaxTiers = 5;
    public const int MaxHours = 8760;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static List<RoomRentalCancellationTierDto> Defaults(CinemaSettingsSnapshot settings)
    {
        var tiers = new List<RoomRentalCancellationTierDto>
        {
            new(settings.RoomRentalFullRefundDays * 24, 100),
        };
        if (settings.RoomRentalPartialRefundDays > 0 && settings.RoomRentalPartialRefundPercent > 0)
            tiers.Add(
                new RoomRentalCancellationTierDto(
                    settings.RoomRentalPartialRefundDays * 24,
                    settings.RoomRentalPartialRefundPercent
                )
            );
        return Normalize(tiers);
    }

    public static List<RoomRentalCancellationTierDto> Normalize(
        IEnumerable<RoomRentalCancellationTierDto> tiers
    ) => [.. tiers.OrderByDescending(t => t.MinHoursBeforeStart)];

    public static bool IsValid(IReadOnlyCollection<RoomRentalCancellationTierDto>? tiers)
    {
        if (tiers is null)
            return true;
        if (tiers.Count > MaxTiers)
            return false;
        if (tiers.Any(t => t.MinHoursBeforeStart is < 1 or > MaxHours || t.RefundPercent is < 0 or > 100))
            return false;
        if (tiers.Select(t => t.MinHoursBeforeStart).Distinct().Count() != tiers.Count)
            return false;

        var ordered = Normalize(tiers);
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].RefundPercent > ordered[i - 1].RefundPercent)
                return false;
        }

        return true;
    }

    public static string Serialize(IEnumerable<RoomRentalCancellationTierDto> tiers) =>
        JsonSerializer.Serialize(Normalize(tiers), JsonOptions);

    public static List<RoomRentalCancellationTierDto> Parse(string json) =>
        Normalize(
            JsonSerializer.Deserialize<List<RoomRentalCancellationTierDto>>(json, JsonOptions)
                ?? []
        );

    public static int RefundPercentFor(
        IEnumerable<RoomRentalCancellationTierDto> tiers,
        double hoursBeforeStart
    ) =>
        Normalize(tiers).FirstOrDefault(t => hoursBeforeStart >= t.MinHoursBeforeStart)?.RefundPercent
        ?? 0;
}
