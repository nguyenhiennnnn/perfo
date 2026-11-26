using Cinema.Domain.Enums;

namespace Cinema.Application.Services.RoomRentals;

public static class RoomRentalStatusRules
{
    private static readonly IReadOnlyDictionary<
        RoomRentalRequestStatus,
        RoomRentalRequestStatus[]
    > ManualTransitions = new Dictionary<RoomRentalRequestStatus, RoomRentalRequestStatus[]>
    {
        [RoomRentalRequestStatus.New] =
        [
            RoomRentalRequestStatus.Processing,
            RoomRentalRequestStatus.Cancelled,
        ],
        [RoomRentalRequestStatus.Processing] = [RoomRentalRequestStatus.Cancelled],
        [RoomRentalRequestStatus.Quoted] =
        [
            RoomRentalRequestStatus.Processing,
            RoomRentalRequestStatus.Cancelled,
        ],
        [RoomRentalRequestStatus.Accepted] = [RoomRentalRequestStatus.Cancelled],
        [RoomRentalRequestStatus.DepositPaid] = [RoomRentalRequestStatus.Cancelled],
        [RoomRentalRequestStatus.FullyPaid] =
        [
            RoomRentalRequestStatus.Completed,
            RoomRentalRequestStatus.Cancelled,
        ],
        [RoomRentalRequestStatus.QuoteExpired] =
        [
            RoomRentalRequestStatus.Processing,
            RoomRentalRequestStatus.Cancelled,
        ],
    };

    public static IReadOnlyList<RoomRentalRequestStatus> AllowedNext(
        RoomRentalRequestStatus from
    ) => ManualTransitions.TryGetValue(from, out var next) ? next : [];

    public static bool CanChange(RoomRentalRequestStatus from, RoomRentalRequestStatus to) =>
        AllowedNext(from).Contains(to);
}
