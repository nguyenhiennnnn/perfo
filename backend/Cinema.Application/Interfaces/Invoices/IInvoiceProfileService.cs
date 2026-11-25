using Cinema.Application.DTOs.Invoices;

namespace Cinema.Application.Interfaces.Invoices;

public interface IInvoiceProfileService
{
    Task<IReadOnlyList<InvoiceProfileDto>> GetMineAsync(
        int userId,
        CancellationToken cancellationToken
    );

    Task<InvoiceProfileDto> CreateAsync(
        int userId,
        UpsertInvoiceProfileRequest request,
        CancellationToken cancellationToken
    );

    Task<InvoiceProfileDto> UpdateAsync(
        int userId,
        int profileId,
        UpsertInvoiceProfileRequest request,
        CancellationToken cancellationToken
    );

    Task<DeleteInvoiceProfileResponse> DeleteAsync(
        int userId,
        int profileId,
        CancellationToken cancellationToken
    );
}
