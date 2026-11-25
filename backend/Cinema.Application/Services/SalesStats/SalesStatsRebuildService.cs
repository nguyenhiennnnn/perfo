using System.Globalization;
using Cinema.Application.BackgroundJobs;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.ImportExport;
using Cinema.Application.DTOs.SalesStats;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Options;
using Cinema.Application.Services.ImportExport;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.SalesStats;

public class SalesStatsRebuildService(
    IAppDbContext db,
    IImportExportJobTracker tracker,
    IImportExportConcurrencyGuard concurrencyGuard,
    IBackgroundJobClient backgroundJobClient,
    IOptions<ImportExportOptions> options
) : ISalesStatsRebuildService
{
    public const string DateFormat = DateTimePatterns.IsoDate;
    public const int MaxDays = 366;
    private const string FormatName = "db";

    public async Task<ImportExportJobAcceptedDto> EnqueueAsync(
        ActorContext actor,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken
    )
    {
        if (fromDate > toDate)
            throw new BusinessRuleException(Messages.Rule_SalesStatsRebuildRangeInvalid);

        var totalDays = toDate.DayNumber - fromDate.DayNumber + 1;
        if (totalDays > MaxDays)
            throw new BusinessRuleException(
                string.Format(Messages.Rule_SalesStatsRebuildRangeTooLong, MaxDays)
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
                    && j.Module == ImportExportModules.SalesStatsRebuild
                    && (
                        j.Status == ImportExportJobStatus.Queued
                        || j.Status == ImportExportJobStatus.Running
                    )
                    && j.CreatedAt > staleBefore,
                cancellationToken
            );
        if (alreadyRunning)
            throw new BusinessRuleException(Messages.Rule_SalesStatsRebuildAlreadyRunning);

        var from = fromDate.ToString(DateFormat, CultureInfo.InvariantCulture);
        var to = toDate.ToString(DateFormat, CultureInfo.InvariantCulture);

        var jobId = await tracker.CreateQueuedAsync(
            ImportExportJobType.Rebuild,
            ImportExportModules.SalesStatsRebuild,
            FormatName,
            actor,
            null,
            totalDays,
            ImportExportJson.Serialize(new SalesStatsRebuildParameters(from, to)),
            cancellationToken
        );

        try
        {
            backgroundJobClient.Enqueue<SalesStatsRebuildJob>(j =>
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
