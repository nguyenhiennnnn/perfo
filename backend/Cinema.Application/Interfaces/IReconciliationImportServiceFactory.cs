using Cinema.Application.DTOs.Reconciliation;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

public interface IReconciliationImportServiceFactory
{
    IImportService<ReconciliationImportRow> Create(ReconciliationGateway gateway, int userId);

    IImportService<ReconciliationImportRow> CreateForSession();
}
