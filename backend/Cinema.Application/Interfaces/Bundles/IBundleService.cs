using Cinema.Application.DTOs.Bundles;
using Cinema.Application.DTOs.Common;

namespace Cinema.Application.Interfaces.Bundles;

public interface IBundleService
{
    Task<OffsetPageResult<BundleDto>> GetAdminAsync(
        int page,
        int pageSize,
        BundleListFilter filter,
        CancellationToken cancellationToken
    );

    Task<BundleDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<BundleDto> CreateAsync(UpsertBundleRequest request, CancellationToken cancellationToken);

    Task<BundleDto> UpdateAsync(
        int id,
        UpsertBundleRequest request,
        CancellationToken cancellationToken
    );

    Task<DeleteBundleResponse> DeleteAsync(int id, CancellationToken cancellationToken);
}
