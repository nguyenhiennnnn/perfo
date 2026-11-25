using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.ImportExport;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.ImportExport;

public class ImportExportJobTracker(
    IServiceScopeFactory scopeFactory,
    ILogger<ImportExportJobTracker> logger
) : IImportExportJobTracker
{
    private const int MaxFileNameLength = FieldLengths.FileName;
    private const int MaxErrorLength = FieldLengths.ErrorMessage;
    private const int MaxRoleLength = FieldLengths.ActorRole;
    private const int MaxIpLength = FieldLengths.IpAddress;
    private const int MaxUserAgentLength = FieldLengths.UserAgent;
    private const int MaxParametersLength = FieldLengths.JobParameters;

    public async Task<Guid> CreateQueuedAsync(
        ImportExportJobType type,
        string module,
        string format,
        ActorContext actor,
        string? fileName,
        int totalRows,
        string? parameters,
        CancellationToken cancellationToken
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var job = NewJob(type, module, format, actor, fileName, parameters);
        job.Status = ImportExportJobStatus.Queued;
        job.Progress = 0;
        job.TotalRows = totalRows;

        db.ImportExportJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);

        Audit("Queued", job);
        return job.JobId;
    }

    public async Task<ImportExportJobRunContext?> MarkRunningAsync(
        Guid jobId,
        CancellationToken cancellationToken
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var job = await FindAsync(db, jobId, cancellationToken);
        if (job is null)
            return null;

        job.Status = ImportExportJobStatus.Running;
        job.Progress = 0;
        job.StartedAt = DateTime.UtcNow;
        job.CompletedAt = null;
        job.ErrorMessage = null;
        await db.SaveChangesAsync(cancellationToken);

        Audit("Started", job);

        await NotifyAsync(
            scope,
            job,
            (notifier, dto) =>
                notifier.NotifyImportExportJobProgressAsync(
                    job.CreatedByUserId,
                    dto,
                    cancellationToken
                )
        );

        return new ImportExportJobRunContext(job.CreatedByUserId, job.TotalRows, job.ActorRole);
    }

    public async Task ReportProgressAsync(
        Guid jobId,
        int progress,
        CancellationToken cancellationToken
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var job = await FindAsync(db, jobId, cancellationToken);
        if (job is null || job.Status != ImportExportJobStatus.Running)
            return;

        job.Progress = Math.Clamp(progress, 0, 100);
        await db.SaveChangesAsync(cancellationToken);

        await NotifyAsync(
            scope,
            job,
            (notifier, dto) =>
                notifier.NotifyImportExportJobProgressAsync(
                    job.CreatedByUserId,
                    dto,
                    cancellationToken
                )
        );
    }

    public async Task CompleteExportAsync(
        Guid jobId,
        string fileName,
        FileUploadResult file,
        long? fileSizeBytes,
        int totalRows,
        int retentionHours,
        CancellationToken cancellationToken
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var job = await FindAsync(db, jobId, cancellationToken);
        if (job is null)
            return;

        var now = DateTime.UtcNow;
        job.Status = ImportExportJobStatus.Completed;
        job.Progress = 100;
        job.TotalRows = totalRows;
        job.SuccessRows = totalRows;
        job.FailedRows = 0;
        job.FileName = Truncate(fileName, MaxFileNameLength);
        job.FileSizeBytes = fileSizeBytes;
        job.StoragePath = file.PublicId;
        job.FileUrl = file.Url;
        job.ErrorMessage = null;
        job.CompletedAt = now;
        job.ExpiresAt = now.AddHours(retentionHours);
        await db.SaveChangesAsync(cancellationToken);

        Audit("Completed", job);

        await NotifyAsync(
            scope,
            job,
            (notifier, dto) =>
                notifier.NotifyImportExportJobCompletedAsync(
                    job.CreatedByUserId,
                    dto,
                    cancellationToken
                )
        );
    }

    public async Task CompleteImportAsync(
        Guid jobId,
        int successRows,
        int failedRows,
        string? errorMessage,
        CancellationToken cancellationToken
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var job = await FindAsync(db, jobId, cancellationToken);
        if (job is null)
            return;

        var failed = ResolveImportStatus(successRows, failedRows) == ImportExportJobStatus.Failed;
        job.Status = failed ? ImportExportJobStatus.Failed : ImportExportJobStatus.Completed;
        job.Progress = 100;
        job.TotalRows = successRows + failedRows;
        job.SuccessRows = successRows;
        job.FailedRows = failedRows;
        job.ErrorMessage = failed ? Truncate(errorMessage, MaxErrorLength) : null;
        job.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        Audit(failed ? "Failed" : "Completed", job);

        await NotifyAsync(
            scope,
            job,
            (notifier, dto) =>
                failed
                    ? notifier.NotifyImportExportJobFailedAsync(
                        job.CreatedByUserId,
                        dto,
                        cancellationToken
                    )
                    : notifier.NotifyImportExportJobCompletedAsync(
                        job.CreatedByUserId,
                        dto,
                        cancellationToken
                    )
        );
    }

    public async Task FailAsync(
        Guid jobId,
        string errorMessage,
        CancellationToken cancellationToken
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var job = await FindAsync(db, jobId, cancellationToken);
        if (job is null)
            return;

        job.Status = ImportExportJobStatus.Failed;
        job.ErrorMessage = Truncate(errorMessage, MaxErrorLength);
        job.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        Audit("Failed", job);

        await NotifyAsync(
            scope,
            job,
            (notifier, dto) =>
                notifier.NotifyImportExportJobFailedAsync(
                    job.CreatedByUserId,
                    dto,
                    cancellationToken
                )
        );
    }

    public async Task RecordCompletedAsync(
        ImportExportJobType type,
        string module,
        string format,
        ActorContext actor,
        string? fileName,
        int totalRows,
        int successRows,
        int failedRows,
        string? parameters,
        long? fileSizeBytes,
        CancellationToken cancellationToken
    )
    {
        var job = NewJob(type, module, format, actor, fileName, parameters);
        var now = job.CreatedAt;
        job.Status =
            type == ImportExportJobType.Import
                ? ResolveImportStatus(successRows, failedRows)
                : ImportExportJobStatus.Completed;
        job.Progress = 100;
        job.TotalRows = totalRows;
        job.SuccessRows = successRows;
        job.FailedRows = failedRows;
        job.FileSizeBytes = fileSizeBytes;
        job.StartedAt = now;
        job.CompletedAt = now;

        await PersistRecordAsync(job, cancellationToken);
    }

    public async Task RecordFailedAsync(
        ImportExportJobType type,
        string module,
        string format,
        ActorContext actor,
        string? fileName,
        string? parameters,
        string errorMessage,
        CancellationToken cancellationToken
    )
    {
        var job = NewJob(type, module, format, actor, fileName, parameters);
        job.Status = ImportExportJobStatus.Failed;
        job.Progress = 100;
        job.ErrorMessage = Truncate(errorMessage, MaxErrorLength);
        job.StartedAt = job.CreatedAt;
        job.CompletedAt = job.CreatedAt;

        await PersistRecordAsync(job, cancellationToken);
    }

    private async Task PersistRecordAsync(ImportExportJob job, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            db.ImportExportJobs.Add(job);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "Không ghi được lịch sử {Type} {Module} của UserId {UserId}",
                job.Type,
                job.Module,
                job.CreatedByUserId
            );
        }

        Audit(job.Status == ImportExportJobStatus.Failed ? "Failed" : "Completed", job);
    }

    private static ImportExportJob NewJob(
        ImportExportJobType type,
        string module,
        string format,
        ActorContext actor,
        string? fileName,
        string? parameters
    ) =>
        new()
        {
            JobId = Guid.NewGuid(),
            Type = type,
            Module = module,
            Format = format,
            FileName = Truncate(fileName, MaxFileNameLength),
            CreatedByUserId = actor.UserId,
            ActorRole = Truncate(actor.Role, MaxRoleLength),
            IpAddress = Truncate(actor.IpAddress, MaxIpLength),
            UserAgent = Truncate(actor.UserAgent, MaxUserAgentLength),
            Parameters = Truncate(parameters, MaxParametersLength),
            IsSensitive = ImportExportModules.IsSensitive(module),
            IsPiiMasked =
                type == ImportExportJobType.Export
                && !actor.IsAdmin
                && ImportExportModules.ContainsPersonalData(module),
            CreatedAt = DateTime.UtcNow,
        };

    private void Audit(string auditEvent, ImportExportJob job)
    {
        var level = job.Status == ImportExportJobStatus.Failed ? LogLevel.Warning : LogLevel.Information;

        logger.Log(
            level,
            "ImportExportAudit {AuditEvent} JobId {JobId} Type {JobType} Module {Module} Format {Format} "
                + "Status {Status} UserId {UserId} Role {Role} Ip {IpAddress} UserAgent {UserAgent} "
                + "Sensitive {IsSensitive} PiiMasked {IsPiiMasked} TotalRows {TotalRows} "
                + "SuccessRows {SuccessRows} FailedRows {FailedRows} FileName {FileName} "
                + "FileSizeBytes {FileSizeBytes} Parameters {Parameters} Error {ErrorMessage}",
            auditEvent,
            job.JobId,
            job.Type,
            job.Module,
            job.Format,
            job.Status,
            job.CreatedByUserId,
            job.ActorRole,
            job.IpAddress,
            job.UserAgent,
            job.IsSensitive,
            job.IsPiiMasked,
            job.TotalRows,
            job.SuccessRows,
            job.FailedRows,
            job.FileName,
            job.FileSizeBytes,
            job.Parameters,
            job.ErrorMessage
        );
    }

    private static ImportExportJobStatus ResolveImportStatus(int successRows, int failedRows) =>
        successRows == 0 && failedRows > 0
            ? ImportExportJobStatus.Failed
            : ImportExportJobStatus.Completed;

    private static Task<ImportExportJob?> FindAsync(
        IAppDbContext db,
        Guid jobId,
        CancellationToken cancellationToken
    ) => db.ImportExportJobs.FirstOrDefaultAsync(j => j.JobId == jobId, cancellationToken);

    private async Task NotifyAsync(
        AsyncServiceScope scope,
        ImportExportJob job,
        Func<IRealtimeNotifier, ImportExportJobDto, Task> send
    )
    {
        try
        {
            var notifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();
            await send(notifier, job.ToDto(DateTime.UtcNow));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Không đẩy được realtime cho JobId {JobId}", job.JobId);
        }
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
