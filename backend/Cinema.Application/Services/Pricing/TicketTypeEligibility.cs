using System.Globalization;
using Cinema.Application.Caching;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Pricing;

public static class TicketTypeEligibility
{
    public static string? IneligibleReason(
        TicketTypeSnapshot ticketType,
        int? movieAgeRatingId,
        TimeOnly localTime,
        DateOnly businessDate
    )
    {
        if (
            ticketType.AllowedAgeRatingIds.Count > 0
            && (
                movieAgeRatingId is null
                || !ticketType.AllowedAgeRatingIds.Contains(movieAgeRatingId.Value)
            )
        )
            return Messages.Pricing_Ineligible_AgeRating;

        if (
            ticketType.AllowedFromTime is { } from
            && ticketType.AllowedToTime is { } to
            && (localTime < from || localTime >= to)
        )
            return string.Format(
                Messages.Pricing_Ineligible_TimeWindow,
                from.ToString(DateTimePatterns.HourMinute, CultureInfo.InvariantCulture),
                to.ToString(DateTimePatterns.HourMinute, CultureInfo.InvariantCulture)
            );

        if (
            ticketType.AllowedDaysOfWeek.Count > 0
            && !ticketType.AllowedDaysOfWeek.Contains((int)businessDate.DayOfWeek)
        )
            return Messages.Pricing_Ineligible_DayOfWeek;

        return null;
    }
}
