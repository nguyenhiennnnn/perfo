namespace Cinema.Application.Services.Pricing;

public static class PricingCalendar
{
    public static DateOnly BusinessDate(DateTime localStart, TimeOnly? opening, TimeOnly? closing)
    {
        var date = DateOnly.FromDateTime(localStart);
        return opening is { } open
            && closing is { } close
            && close <= open
            && TimeOnly.FromDateTime(localStart) < close
            ? date.AddDays(-1)
            : date;
    }
}
