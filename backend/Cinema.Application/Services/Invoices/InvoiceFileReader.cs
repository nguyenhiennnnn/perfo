using Cinema.Application.DTOs.Invoices;
using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Invoices;

public static class InvoiceFileReader
{
    public static async Task<InvoiceFileResult> OpenAsync(
        IFileStorage fileStorage,
        InvoiceRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.Status != InvoiceRequestStatus.Issued || string.IsNullOrEmpty(request.FileUrl))
            throw new NotFoundException(Messages.NotFound_InvoiceFile);

        var stream = await fileStorage.OpenReadAsync(request.FileUrl, cancellationToken);
        return new InvoiceFileResult(stream, request.FileName ?? $"invoice-{request.InvoiceNumber}.pdf");
    }
}
