using Cinema.Domain.Entities;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cinema.Infrastructure.Persistence;

public sealed class AuditLogImmutabilityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        EnsureAppendOnly(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        EnsureAppendOnly(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void EnsureAppendOnly(DbContext? context)
    {
        if (context is null)
            return;

        var violated = context
            .ChangeTracker.Entries<AuditLog>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);
        if (violated)
            throw new InvalidOperationException(Messages.Rule_AuditLogImmutable);
    }
}
