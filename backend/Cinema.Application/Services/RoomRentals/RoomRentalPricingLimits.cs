namespace Cinema.Application.Services.RoomRentals;

public static class RoomRentalPricingLimits
{
    public const int MaxDurationMinutes = 1440;
    public const decimal MaxAmount = 1_000_000_000m;
    public const int MaxComboItems = 20;
    public const int MaxComboQuantity = 1000;
    public const int MaxExtraItems = 20;
    public const int MaxExtraQuantity = 1000;
    public const int MaxLabelLength = 150;
    public const int MaxDiscountReasonLength = 150;
}
