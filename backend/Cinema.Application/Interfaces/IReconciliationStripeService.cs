using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.ImportExport;

namespace Cinema.Application.Interfaces;

public interface IReconciliationStripeService
{
    Task<ImportExportJobAcceptedDto> EnqueueAsync(
        ActorContext actor,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken
    );
}
