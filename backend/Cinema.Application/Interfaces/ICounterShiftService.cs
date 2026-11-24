using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.CounterShifts;

namespace Cinema.Application.Interfaces;

public interface ICounterShiftService
{
    Task<OffsetPageResult<CounterShiftDto>> GetShiftsAsync(
        CounterShiftActor actor,
        CounterShiftListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<CounterShiftDto> GetByIdAsync(
        CounterShiftActor actor,
        int id,
        CancellationToken cancellationToken
    );

    Task<CounterShiftDto> OpenAsync(
        CounterShiftActor actor,
        OpenCounterShiftRequest request,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<OpenCounterShiftItemDto>> GetOpenShiftsAsync(
        CancellationToken cancellationToken
    );

    Task<CounterShiftDto> CloseByAdminAsync(
        CounterShiftActor actor,
        int id,
        AdminCloseCounterShiftRequest request,
        CancellationToken cancellationToken
    );

    Task<CounterShiftDto> CloseAsync(
        CounterShiftActor actor,
        int id,
        CloseCounterShiftRequest request,
        CancellationToken cancellationToken
    );
}
