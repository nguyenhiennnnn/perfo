using System.Globalization;
using Cinema.Application.BackgroundJobs;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.ImportExport;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reconciliation;
using Cinema.Application.Options;
using Cinema.Application.Services.ImportExport;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Reconciliation;

public class ReconciliationStripeService(
    IAppDbContext db,
    IImportExportJobTracker tracker,
    IImportExportConcurrencyGuard concurrencyGuard,
    IBackgroundJobClient backgroundJobClient,
    IOptions<ImportExportOptions> options
) : IReconciliationStripeService
{
    public const string DateFormat = DateTimePatterns.IsoDate;
    public const int MaxDays = 31;
    private const string FormatName = "api";

    public async Task<ImportExportJobAcceptedDto> EnqueueAsync(
        ActorContext actor,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken
    )
    {
        if (fromDate > toDate)
            throw new BusinessRuleException(Messages.Rule_ReconciliationStripeRangeInvalid);

        var totalDays = toDate.DayNumber - fromDate.DayNumber + 1;
        if (totalDays > MaxDays)
            throw new BusinessRuleException(
                string.Format(Messages.Rule_ReconciliationStripeRangeTooLong, MaxDays)
            );

        using var lease = await concurrencyGuard.AcquireAsync(actor.UserId, cancellationToken);

        var staleBefore = DateTime.UtcNow.AddMinutes(
            -Math.Max(1, options.Value.ActiveJobStaleMinutes)
        );
        var alreadyRunning = await db
            .ImportExportJobs.AsNoTracking()
            .AnyAsync(
                j =>
                    j.Type == ImportExportJobType.Rebuild
                    && j.Module == ImportExportModules.ReconciliationStripe
                    && (
                        j.Status == ImportExportJobStatus.Queued
                        || j.Status == ImportExportJobStatus.Running
                    )
                    && j.CreatedAt > staleBefore,
                cancellationToken
            );
        if (alreadyRunning)
            throw new BusinessRuleException(Messages.Rule_ReconciliationStripeAlreadyRunning);

        var from = fromDate.ToString(DateFormat, CultureInfo.InvariantCulture);
        var to = toDate.ToString(DateFormat, CultureInfo.InvariantCulture);

        var jobId = await tracker.CreateQueuedAsync(
            ImportExportJobType.Rebuild,
            ImportExportModules.ReconciliationStripe,
            FormatName,
            actor,
            null,
            totalDays,
            ImportExportJson.Serialize(new { fromDate = from, toDate = to }),
            cancellationToken
        );

        try
        {
            backgroundJobClient.Enqueue<ReconciliationStripeJob>(j =>
                j.RunAsync(jobId, from, to, CancellationToken.None)
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await tracker.FailAsync(
                jobId,
                ImportExportJobErrors.Describe(ex),
                CancellationToken.None
            );
            throw;
        }

        return new ImportExportJobAcceptedDto(jobId);
    }
}
