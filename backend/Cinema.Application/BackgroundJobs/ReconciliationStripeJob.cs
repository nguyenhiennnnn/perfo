using System.Globalization;
using Cinema.Application.Caching;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.ImportExport;
using Cinema.Application.Services.Reconciliation;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.BackgroundJobs;

public class ReconciliationStripeJob(
    IServiceScopeFactory scopeFactory,
    IImportExportJobTracker tracker,
    ILogger<ReconciliationStripeJob> logger
)
{
    private const int MaxFetchProgress = 90;

    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    public async Task RunAsync(
        Guid jobId,
        string fromDate,
        string toDate,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var context = await tracker.MarkRunningAsync(jobId, cancellationToken);
            if (context is null)
            {
                logger.LogWarning("ReconciliationStripeJob: JobId {JobId} không tồn tại.", jobId);
                return;
            }

            var from = DateOnly.ParseExact(
                fromDate,
                ReconciliationStripeService.DateFormat,
                CultureInfo.InvariantCulture
            );
            var to = DateOnly.ParseExact(
                toDate,
                ReconciliationStripeService.DateFormat,
                CultureInfo.InvariantCulture
            );

            await using var scope = scopeFactory.CreateAsyncScope();
            var sp = scope.ServiceProvider;
            var gateway =
                sp.GetServices<IPaymentGateway>().FindByMethod(PaymentMethodNames.Stripe)
                ?? throw new NotSupportedException(PaymentMethodNames.Stripe);
            var settings = await sp.GetRequiredService<IAppCache>()
                .GetAsync(CacheKeys.CinemaSettings, cancellationToken);
            var fromUtc = BusinessDay.StartUtc(from, settings.OpeningTime, settings.ClosingTime);
            var toUtc = BusinessDay.EndUtc(to, settings.OpeningTime, settings.ClosingTime);

            var lines = await gateway.ListStatementLinesAsync(
                fromUtc,
                toUtc,
                (fraction, ct) =>
                    tracker.ReportProgressAsync(jobId, (int)(fraction * MaxFetchProgress), ct),
                cancellationToken
            );

            var result = await sp.GetRequiredService<IReconciliationService>()
                .RunAsync(
                    new ReconciliationRequest(
                        ReconciliationGateway.Stripe,
                        ReconciliationSource.Api,
                        null,
                        null,
                        from,
                        to,
                        lines,
                        context.UserId
                    ),
                    cancellationToken
                );

            await tracker.CompleteImportAsync(
                jobId,
                result.TotalRows,
                0,
                null,
                CancellationToken.None
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "ReconciliationStripeJob thất bại JobId {JobId}", jobId);
            await tracker.FailAsync(
                jobId,
                ImportExportJobErrors.Describe(ex),
                CancellationToken.None
            );
            throw;
        }
    }
}
