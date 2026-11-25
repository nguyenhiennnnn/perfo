using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Ledger;

namespace Cinema.Application.Interfaces.Ledger;

public interface ILedgerAdjustmentService
{
    Task<OffsetPageResult<LedgerAdjustmentDto>> GetAdjustmentsAsync(
        LedgerAdjustmentListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<LedgerAdjustmentDto> CreateManualAsync(
        int userId,
        CreateLedgerAdjustmentRequest request,
        CancellationToken cancellationToken
    );
}
