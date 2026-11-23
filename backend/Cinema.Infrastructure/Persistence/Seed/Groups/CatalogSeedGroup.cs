using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class CatalogSeedGroup : ISeedGroup
{
    private sealed record AgeRatingSeed(string Code, string Description);

    private static readonly string[] Genres =
    [
        "Hành động",
        "Hài",
        "Chính kịch",
        "Kinh dị",
        "Khoa học viễn tưởng",
        "Hoạt hình",
        "Lãng mạn",
        "Tâm lý",
        "Phiêu lưu",
        "Bí ẩn",
        "Gia đình",
        "Tâm linh",
    ];

    private static readonly string[] Languages =
    [
        "Tiếng Việt",
        "Tiếng Anh",
        "Tiếng Hàn",
        "Tiếng Nhật",
        "Tiếng Thái",
    ];

    private static readonly AgeRatingSeed[] AgeRatings =
    [
        new("P", "Phổ biến, không hạn chế đối tượng xem"),
        new("K", "Phổ biến, khuyến cáo trẻ dưới 13 tuổi xem cùng cha mẹ"),
        new("T13", "Cấm phổ biến với khán giả dưới 13 tuổi"),
        new("T16", "Cấm phổ biến với khán giả dưới 16 tuổi"),
        new("T18", "Cấm phổ biến với khán giả dưới 18 tuổi"),
        new("C", "Không được phép phổ biến"),
    ];

    public string Name => "catalog";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existingGenres = (
            await db.Genres.Select(g => g.Name).ToListAsync(cancellationToken)
        ).ToHashSet();
        var newGenres = Genres
            .Where(name => !existingGenres.Contains(name))
            .Select(name => new Genre { Name = name })
            .ToList();

        var existingLanguages = (
            await db.Languages.Select(l => l.Name).ToListAsync(cancellationToken)
        ).ToHashSet();
        var newLanguages = Languages
            .Where(name => !existingLanguages.Contains(name))
            .Select(name => new Language { Name = name })
            .ToList();

        var existingRatings = (
            await db.AgeRatings.Select(a => a.Code).ToListAsync(cancellationToken)
        ).ToHashSet();
        var newRatings = AgeRatings
            .Where(seed => !existingRatings.Contains(seed.Code))
            .Select(seed => new AgeRating { Code = seed.Code, Description = seed.Description })
            .ToList();

        if (newGenres.Count == 0 && newLanguages.Count == 0 && newRatings.Count == 0)
            return;

        db.Genres.AddRange(newGenres);
        db.Languages.AddRange(newLanguages);
        db.AgeRatings.AddRange(newRatings);
        await db.SaveChangesAsync(cancellationToken);

        context.Logger.LogInformation(
            "Seeded {Genres} genres, {Languages} languages, {Ratings} age ratings.",
            newGenres.Count,
            newLanguages.Count,
            newRatings.Count
        );
    }
}
