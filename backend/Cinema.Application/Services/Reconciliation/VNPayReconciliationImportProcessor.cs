using Cinema.Application.Interfaces.Import;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reconciliation;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Reconciliation;

public sealed class VNPayReconciliationImportProcessor(
    IImportFileReader fileReader,
    IExportWriter exportWriter,
    IAppCache cache,
    IReconciliationService reconciliation,
    int userId
) : ReconciliationImportProcessorBase(fileReader, exportWriter, cache, reconciliation, userId)
{
    public override ReconciliationGateway Gateway => ReconciliationGateway.VNPay;

    protected override IReadOnlySet<string> SuccessStatuses { get; } =
        new HashSet<string> { "00", "0", "thanhcong", "success", "giaodichthanhcong" };

    protected override string[] TimeFormats { get; } =
        ["yyyyMMddHHmmss", "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "yyyy-MM-dd HH:mm:ss"];

    protected override IReadOnlyList<IReadOnlyList<object?>> ExampleRows { get; } =
        [
            new object?[] { "20261124T0001", "150000", "2250", "00", "20261124183015" },
            new object?[] { "20261124T0002", "90000", "1350", "Thành công", "24/11/2026 19:02:41" },
        ];
}
