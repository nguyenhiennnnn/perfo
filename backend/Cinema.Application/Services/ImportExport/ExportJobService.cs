using System.Globalization;
using Cinema.Application.BackgroundJobs;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.ImportExport;
using Cinema.Application.Interfaces;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.ImportExport;

public class ExportJobService(
    IEnumerable<IExportModule> modules,
    IImportExportJobTracker tracker,
    IImportExportConcurrencyGuard concurrencyGuard,
    IBackgroundJobClient backgroundJobClient,
    IOptions<ImportExportOptions> options
) : IExportJobService
{
    public async Task<ExportFileDto> ExportNowAsync<TFilter>(
        ActorContext actor,
        string module,
        TFilter filter,
        ExportFormat format,
        CancellationToken cancellationToken
    )
    {
        var handler = Resolve(module);
        var filterJson = ImportExportJson.Serialize(filter);
        var threshold = options.Value.ExportBackgroundThreshold;

        using var lease = await concurrencyGuard.AcquireAsync(actor.UserId, cancellationToken);

        var estimated = await handler.CountAsync(filterJson, cancellationToken);
        if (estimated > threshold)
            throw new BusinessRuleException(
                string.Format(
                    Messages.Rule_ExportUseBackgroundJob,
                    threshold.ToString("N0", CultureInfo.GetCultureInfo(RegionalDefaults.Locale))
                )
            );

        return await ExecuteTrackedAsync(
            actor,
            module,
            format,
            filterJson,
            (context, counter) =>
                handler.ExportAsync(filterJson, format, context, counter, cancellationToken),
            cancellationToken
        );
    }

    public async Task<ExportFileDto> RunTrackedAsync<TParameters>(
        ActorContext actor,
        string module,
        ExportFormat format,
        TParameters parameters,
        Func<ExportContext, IJobProgressReporter, Task<ExportFileDto>> export,
        CancellationToken cancellationToken
    )
    {
        using var lease = await concurrencyGuard.AcquireAsync(actor.UserId, cancellationToken);

        return await ExecuteTrackedAsync(
            actor,
            module,
            format,
            ImportExportJson.Serialize(parameters),
            export,
            cancellationToken
        );
    }

    public async Task<ImportExportJobAcceptedDto> EnqueueAsync<TFilter>(
        ActorContext actor,
        string module,
        TFilter filter,
        ExportFormat format,
        CancellationToken cancellationToken
    )
    {
        var handler = Resolve(module);
        var filterJson = ImportExportJson.Serialize(filter);

        using var lease = await concurrencyGuard.AcquireAsync(actor.UserId, cancellationToken);

        var estimated = await handler.CountAsync(filterJson, cancellationToken);
        ExportLimits.EnsureWithinLimit(estimated);

        var formatName = ExportFormats.Extension(format);
        var jobId = await tracker.CreateQueuedAsync(
            ImportExportJobType.Export,
            module,
            formatName,
            actor,
            null,
            estimated,
            filterJson,
            cancellationToken
        );

        try
        {
            backgroundJobClient.Enqueue<ExportJob>(j =>
                j.RunAsync(jobId, module, filterJson, formatName, CancellationToken.None)
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

    private async Task<ExportFileDto> ExecuteTrackedAsync(
        ActorContext actor,
        string module,
        ExportFormat format,
        string? parametersJson,
        Func<ExportContext, IJobProgressReporter, Task<ExportFileDto>> export,
        CancellationToken cancellationToken
    )
    {
        var formatName = ExportFormats.Extension(format);
        var counter = new RowCountReporter();

        try
        {
            var file = await export(new ExportContext(actor), counter);

            await tracker.RecordCompletedAsync(
                ImportExportJobType.Export,
                module,
                formatName,
                actor,
                file.FileName,
                counter.Count,
                counter.Count,
                0,
                parametersJson,
                file.Content.CanSeek ? file.Content.Length : null,
                CancellationToken.None
            );

            return file;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await tracker.RecordFailedAsync(
                ImportExportJobType.Export,
                module,
                formatName,
                actor,
                null,
                parametersJson,
                ImportExportJobErrors.Describe(ex),
                CancellationToken.None
            );
            throw;
        }
    }

    private IExportModule Resolve(string module) =>
        modules.FirstOrDefault(m => m.Module == module)
        ?? throw new InvalidOperationException(
            string.Format(Messages.Export_ModuleNotRegistered, module)
        );
}
