using Cinema.Application.DTOs.Reconciliation;
using Cinema.Domain.Enums;
using Cinema.Application.Interfaces.Import;


namespace Cinema.Application.Interfaces.Reconciliation;

public interface IReconciliationImportServiceFactory
{
    IImportService<ReconciliationImportRow> Create(ReconciliationGateway gateway, int userId);

    IImportService<ReconciliationImportRow> CreateForSession();
}
