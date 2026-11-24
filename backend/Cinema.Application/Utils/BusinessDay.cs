using Cinema.Application.Services.Pricing;

namespace Cinema.Application.Utils;

public static class BusinessDay
{
    public static DateOnly Of(DateTime utc, TimeOnly? opening, TimeOnly? closing) =>
        PricingCalendar.BusinessDate(BusinessTime.ToLocal(utc), opening, closing);

    public static DateTime StartUtc(DateOnly businessDay, TimeOnly? opening, TimeOnly? closing) =>
        BusinessTime.ToUtc(businessDay, CutOff(opening, closing));

    public static DateTime EndUtc(DateOnly businessDay, TimeOnly? opening, TimeOnly? closing) =>
        StartUtc(businessDay.AddDays(1), opening, closing);

    private static TimeOnly CutOff(TimeOnly? opening, TimeOnly? closing) =>
        opening is { } open && closing is { } close && close <= open ? close : TimeOnly.MinValue;
}
