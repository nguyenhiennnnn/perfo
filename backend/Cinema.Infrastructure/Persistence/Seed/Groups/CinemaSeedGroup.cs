using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class CinemaSeedGroup : ISeedGroup
{
    public string Name => "cinema";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var exists = await context.Db.CinemaSettings.AnyAsync(
            s => s.Id == AppDbContextExtensions.CinemaSettingsId,
            cancellationToken
        );
        if (exists)
            return;

        context.Db.CinemaSettings.Add(
            new CinemaSettings
            {
                Id = AppDbContextExtensions.CinemaSettingsId,
                Name = "Perfo Cinema",
                Address =
                    "Tầng 5, TTTM Perfo Plaza, 120 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
                Phone = "02838221234",
                Email = "cskh@perfocinema.vn",
                TaxCode = "0312345678",
                OpeningTime = new TimeOnly(8, 0),
                ClosingTime = new TimeOnly(23, 59),
                PointsPerAmount = 10000m,
                StopSellingMinutesBefore = 10,
                DefaultEmployeePassword = CinemaSettings.DefaultEmployeePasswordValue,
            }
        );
        await context.Db.SaveChangesAsync(cancellationToken);
        context.Logger.LogInformation("Seeded cinema settings.");
    }
}
