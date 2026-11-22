using Cinema.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Utils;

public static class DbTransactionExtensions
{
    public static async Task InTransactionAsync(
        this IAppDbContext db,
        Func<Task> action,
        CancellationToken cancellationToken
    )
    {
        if (db.Database.CurrentTransaction is not null)
        {
            await action();
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await action();
        await transaction.CommitAsync(cancellationToken);
    }
}
