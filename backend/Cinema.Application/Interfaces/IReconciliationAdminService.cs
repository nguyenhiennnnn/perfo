using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Reconciliation;

namespace Cinema.Application.Interfaces;

public interface IReconciliationAdminService
{
    Task<OffsetPageResult<ReconciliationSessionDto>> GetSessionsAsync(
        ReconciliationSessionListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
    Task<ReconciliationSessionDto> GetSessionAsync(int sessionId, CancellationToken cancellationToken);
    Task<OffsetPageResult<ReconciliationItemDto>> GetItemsAsync(
        ReconciliationItemListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
    Task<ReconciliationItemDto> UpdateItemAsync(
        int itemId,
        UpdateReconciliationItemRequest request,
        int actorUserId,
        CancellationToken cancellationToken
    );
}
