using Cinema.Application.Interfaces.Import;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reconciliation;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Reconciliation;

public sealed class MomoReconciliationImportProcessor(
    IImportFileReader fileReader,
    IExportWriter exportWriter,
    IAppCache cache,
    IReconciliationService reconciliation,
    int userId
) : ReconciliationImportProcessorBase(fileReader, exportWriter, cache, reconciliation, userId)
{
    public override ReconciliationGateway Gateway => ReconciliationGateway.Momo;

    protected override IReadOnlySet<string> SuccessStatuses { get; } =
        new HashSet<string> { "0", "thanhcong", "success", "successful", "thanhtoanthanhcong" };

    protected override string[] TimeFormats { get; } =
        ["dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ss"];

    protected override IReadOnlyList<IReadOnlyList<object?>> ExampleRows { get; } =
        [
            new object?[] { "MM20261124T0001", "150000", "2700", "Thành công", "24/11/2026 18:30:15" },
            new object?[] { "MM20261124T0002", "90000", "1620", "0", "24/11/2026 19:02:41" },
        ];
}
