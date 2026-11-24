using Cinema.Application.DTOs.Reconciliation;
using Cinema.Application.Interfaces;
using Cinema.Application.Options;
using Cinema.Application.Services.Import;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Reconciliation;

public class ReconciliationImportServiceFactory(
    IImportFileReader fileReader,
    IExportWriter exportWriter,
    IAppCache cache,
    IReconciliationService reconciliation,
    IImportSessionStore sessions,
    IImportExportJobTracker jobTracker,
    IImportExportConcurrencyGuard concurrencyGuard,
    IBackgroundJobClient backgroundJobClient,
    IOptions<ImportExportOptions> options,
    ILoggerFactory loggerFactory
) : IReconciliationImportServiceFactory
{
    public IImportService<ReconciliationImportRow> Create(ReconciliationGateway gateway, int userId)
    {
        ReconciliationImportProcessorBase processor = gateway switch
        {
            ReconciliationGateway.VNPay => new VNPayReconciliationImportProcessor(
                fileReader,
                exportWriter,
                cache,
                reconciliation,
                userId
            ),
            ReconciliationGateway.Momo => new MomoReconciliationImportProcessor(
                fileReader,
                exportWriter,
                cache,
                reconciliation,
                userId
            ),
            _ => throw new ValidationAppException(
                Messages.Invalid_Reconciliation_GatewayUnsupported,
                new Dictionary<string, string[]>
                {
                    ["gateway"] = [Messages.Invalid_Reconciliation_GatewayUnsupported],
                }
            ),
        };

        return new ImportService<ReconciliationImportRow>(
            processor,
            sessions,
            exportWriter,
            jobTracker,
            concurrencyGuard,
            backgroundJobClient,
            options,
            loggerFactory.CreateLogger<ImportService<ReconciliationImportRow>>()
        );
    }

    public IImportService<ReconciliationImportRow> CreateForSession() =>
        Create(ReconciliationGateway.VNPay, 0);
}
