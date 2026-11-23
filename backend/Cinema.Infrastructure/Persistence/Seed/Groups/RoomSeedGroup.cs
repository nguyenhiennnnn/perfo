using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class RoomSeedGroup : ISeedGroup
{
    private enum BandKind
    {
        Standard,
        Vip,
        Couple,
        Aisle,
    }

    private sealed record RowBand(BandKind Kind, int Count);

    private sealed record RoomSeed(
        string Name,
        string ScreenType,
        int Cols,
        int[] AisleCols,
        RowBand[] Bands,
        string[] InactiveSeats
    );

    private static readonly RoomSeed[] Rooms =
    [
        new(
            "Cinema Hall 01",
            RoomCatalogSeedGroup.ScreenType2D,
            12,
            [],
            [new(BandKind.Standard, 3), new(BandKind.Vip, 4), new(BandKind.Couple, 1)],
            []
        ),
        new(
            "Cinema Hall 02",
            RoomCatalogSeedGroup.ScreenType2D,
            14,
            [5, 10],
            [new(BandKind.Standard, 3), new(BandKind.Vip, 4), new(BandKind.Couple, 1)],
            ["C2"]
        ),
        new(
            "3D Experience Hall 03",
            RoomCatalogSeedGroup.ScreenType3D,
            13,
            [7],
            [new(BandKind.Standard, 2), new(BandKind.Vip, 4), new(BandKind.Couple, 1)],
            ["D3"]
        ),
        new(
            "IMAX Grand Theatre 04",
            RoomCatalogSeedGroup.ScreenTypeImax,
            18,
            [5, 14],
            [
                new(BandKind.Standard, 4),
                new(BandKind.Aisle, 1),
                new(BandKind.Vip, 5),
                new(BandKind.Couple, 1),
            ],
            []
        ),
    ];

    public string Name => "rooms";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var db = context.Db;

        var screenTypeIds = await db.ScreenTypes.ToDictionaryAsync(
            s => s.Name,
            s => s.ScreenTypeId,
            cancellationToken
        );
        var seatTypeIds = await db.SeatTypes.ToDictionaryAsync(
            s => s.Name,
            s => s.SeatTypeId,
            cancellationToken
        );
        var existingNames = (
            await db.CinemaRooms.Select(r => r.RoomName).ToListAsync(cancellationToken)
        ).ToHashSet();

        foreach (var seed in Rooms.Where(r => !existingNames.Contains(r.Name)))
        {
            var seats = BuildSeats(seed, seatTypeIds);
            db.CinemaRooms.Add(
                new CinemaRoom
                {
                    RoomName = seed.Name,
                    ScreenTypeId = screenTypeIds[seed.ScreenType],
                    Rows = seed.Bands.Sum(b => b.Count),
                    Cols = seed.Cols,
                    TotalSeats = seats.Count(s => s.IsActive && !s.IsAisle),
                    IsActive = true,
                    Seats = seats,
                }
            );
            await db.SaveChangesAsync(cancellationToken);
            context.Logger.LogInformation(
                "Seeded room {Room} ({Seats} seats).",
                seed.Name,
                seats.Count
            );
        }
    }

    private static List<Seat> BuildSeats(RoomSeed room, Dictionary<string, int> seatTypeIds)
    {
        var seats = new List<Seat>();
        var rowIdx = 0;

        foreach (var band in room.Bands)
        {
            for (var i = 0; i < band.Count; i++, rowIdx++)
            {
                var row = ((char)('A' + rowIdx)).ToString();
                var typeName = band.Kind switch
                {
                    BandKind.Standard => RoomCatalogSeedGroup.SeatTypeStandard,
                    BandKind.Vip => RoomCatalogSeedGroup.SeatTypeVip,
                    BandKind.Couple => RoomCatalogSeedGroup.SeatTypeCouple,
                    _ => null,
                };
                var coupleIds = new Dictionary<(int Block, int Pair), Guid>();
                var block = 0;
                var posInBlock = 0;

                for (var col = 1; col <= room.Cols; col++)
                {
                    if (band.Kind == BandKind.Aisle || room.AisleCols.Contains(col))
                    {
                        seats.Add(
                            new Seat
                            {
                                SeatRow = row,
                                SeatNumber = col,
                                SeatTypeId = null,
                                IsActive = true,
                                IsAisle = true,
                            }
                        );
                        if (band.Kind != BandKind.Aisle)
                        {
                            block++;
                            posInBlock = 0;
                        }
                        continue;
                    }

                    Guid? coupleId = null;
                    if (band.Kind == BandKind.Couple)
                    {
                        var key = (block, posInBlock / 2);
                        if (!coupleIds.TryGetValue(key, out var id))
                            coupleIds[key] = id = Guid.NewGuid();
                        coupleId = id;
                    }
                    posInBlock++;

                    seats.Add(
                        new Seat
                        {
                            SeatRow = row,
                            SeatNumber = col,
                            SeatTypeId = seatTypeIds[typeName!],
                            CoupleId = coupleId,
                            IsActive = !room.InactiveSeats.Contains($"{row}{col}"),
                            IsAisle = false,
                        }
                    );
                }
            }
        }

        return seats;
    }
}
