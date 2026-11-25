using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.ImportExport;


namespace Cinema.Application.Interfaces.Reports;

public interface IFinanceReportExportService : IExportSource<FinanceExportFilter> { }
