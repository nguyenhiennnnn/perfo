using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Bundles;

public static class BundleReferences
{
    public static async Task<Dictionary<string, string[]>> FindErrorsAsync(
        IAppDbContext db,
        BundleRetailInput input,
        CancellationToken cancellationToken
    )
    {
        var errors = new Dictionary<string, string[]>();

        var ticketTypeIds = input
            .Items.Where(i => i.TicketTypeId is not null)
            .Select(i => i.TicketTypeId!.Value)
            .Distinct()
            .ToList();
        var comboIds = input
            .Items.Where(i => i.ComboId is not null)
            .Select(i => i.ComboId!.Value)
            .Distinct()
            .ToList();

        if (
            (
                ticketTypeIds.Count > 0
                && await db.TicketTypes.CountAsync(
                    t => ticketTypeIds.Contains(t.TicketTypeId) && t.IsActive,
                    cancellationToken
                ) != ticketTypeIds.Count
            )
            || (
                comboIds.Count > 0
                && await db.Combos.CountAsync(
                    c => comboIds.Contains(c.ComboId) && c.IsActive,
                    cancellationToken
                ) != comboIds.Count
            )
        )
            errors["items"] = [Messages.Invalid_Bundle_ItemNotFound];

        var movies = input.MovieIds.ToList();
        if (
            movies.Count > 0
            && await db.Movies.CountAsync(m => movies.Contains(m.MovieId), cancellationToken)
                != movies.Count
        )
            errors["movieIds"] = [Messages.Invalid_Bundle_ReferenceNotFound];

        var showtimes = input.ShowtimeIds.ToList();
        if (
            showtimes.Count > 0
            && await db.Showtimes.CountAsync(
                s => showtimes.Contains(s.ShowtimeId),
                cancellationToken
            ) != showtimes.Count
        )
            errors["showtimeIds"] = [Messages.Invalid_Bundle_ReferenceNotFound];

        var seatTypes = input.SeatTypeIds.ToList();
        if (
            seatTypes.Count > 0
            && await db.SeatTypes.CountAsync(
                s => seatTypes.Contains(s.SeatTypeId),
                cancellationToken
            ) != seatTypes.Count
        )
            errors["seatTypeIds"] = [Messages.Invalid_Bundle_ReferenceNotFound];

        return errors;
    }
}
