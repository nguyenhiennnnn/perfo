using Cinema.Application.DTOs.Pdf;

namespace Cinema.Application.Interfaces.Infrastructure;

public interface IPdfGenerator
{
    Stream GenerateReport(PdfReportDocument document);

    Stream GenerateSectionedReport(PdfSectionedReportDocument document);

    Stream GenerateTickets(PdfTicketDocument document);
}
