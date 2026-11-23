using Cinema.Application.Interfaces;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Promotions;

public static class PromotionReferences
{
    public static async Task<Dictionary<string, string[]>> FindErrorsAsync(
        IAppDbContext db,
        IEnumerable<int>? movieIds,
        IEnumerable<int>? showtimeIds,
        IEnumerable<int>? screenTypeIds,
        IEnumerable<int>? membershipTierIds,
        CancellationToken cancellationToken
    )
    {
        var errors = new Dictionary<string, string[]>();

        var movies = PromotionMapper.NormalizeIds(movieIds);
        if (
            movies.Count > 0
            && await db.Movies.CountAsync(m => movies.Contains(m.MovieId), cancellationToken)
                != movies.Count
        )
            errors["movieIds"] = [Messages.Invalid_Promotion_ReferenceNotFound];

        var showtimes = PromotionMapper.NormalizeIds(showtimeIds);
        if (
            showtimes.Count > 0
            && await db.Showtimes.CountAsync(
                s => showtimes.Contains(s.ShowtimeId),
                cancellationToken
            ) != showtimes.Count
        )
            errors["showtimeIds"] = [Messages.Invalid_Promotion_ReferenceNotFound];

        var screenTypes = PromotionMapper.NormalizeIds(screenTypeIds);
        if (
            screenTypes.Count > 0
            && await db.ScreenTypes.CountAsync(
                s => screenTypes.Contains(s.ScreenTypeId),
                cancellationToken
            ) != screenTypes.Count
        )
            errors["screenTypeIds"] = [Messages.Invalid_Promotion_ReferenceNotFound];

        var tiers = PromotionMapper.NormalizeIds(membershipTierIds);
        if (
            tiers.Count > 0
            && await db.MembershipTiers.CountAsync(
                t => tiers.Contains(t.TierId),
                cancellationToken
            ) != tiers.Count
        )
            errors["membershipTierIds"] = [Messages.Invalid_Promotion_ReferenceNotFound];

        return errors;
    }
}
