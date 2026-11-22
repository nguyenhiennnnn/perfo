using Cinema.Application.DTOs.Audit;
using Cinema.Application.DTOs.Common;

namespace Cinema.Application.Interfaces;

public interface IAuditLogQueryService
{
    Task<OffsetPageResult<AuditLogListItemDto>> GetAsync(
        AuditLogListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<AuditLogDetailDto> GetDetailAsync(long id, CancellationToken cancellationToken);

    Task<AuditLogFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken);
}
