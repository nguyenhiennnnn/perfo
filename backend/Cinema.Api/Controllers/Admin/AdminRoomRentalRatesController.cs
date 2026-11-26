using Cinema.Api.Filters;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/room-rental-rates")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminRoomRentalRatesController(IRoomRentalRateService roomRentalRateService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? roomId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await roomRentalRateService.GetAdminAsync(
                page,
                pageSize,
                new RoomRentalRateListFilter(search, roomId, isActive),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await roomRentalRateService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_RoomRentalRateCreated))]
    public async Task<IActionResult> Create(
        [FromBody] UpsertRoomRentalRateRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await roomRentalRateService.CreateAsync(request, cancellationToken)
        );

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_RoomRentalRateUpdated))]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertRoomRentalRateRequest request,
        CancellationToken cancellationToken
    ) => Ok(await roomRentalRateService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_RoomRentalRateDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await roomRentalRateService.DeleteAsync(id, cancellationToken));
}
