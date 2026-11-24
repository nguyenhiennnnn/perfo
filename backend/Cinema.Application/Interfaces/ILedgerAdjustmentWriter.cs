using Cinema.Application.DTOs.Ledger;

namespace Cinema.Application.Interfaces;

public interface ILedgerAdjustmentWriter
{
    Task<bool> RecordIfOriginalDayClosedAsync(
        LedgerAdjustmentInput input,
        CancellationToken cancellationToken
    );
}
