using Cinema.Api.Filters;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/room-rental-requests")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminRoomRentalRequestsController(
    IRoomRentalAdminService roomRentalAdminService,
    IRoomRentalCancellationService roomRentalCancellationService
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] RoomRentalRequestStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await roomRentalAdminService.GetAsync(
                new RoomRentalAdminListFilter(status, search),
                page,
                pageSize,
                cancellationToken
            )
        );

    [HttpGet("{requestId:int}")]
    public async Task<ActionResult<RoomRentalAdminDto>> Get(
        int requestId,
        CancellationToken cancellationToken
    ) => Ok(await roomRentalAdminService.GetByIdAsync(requestId, cancellationToken));

    [HttpPut("{requestId:int}/status")]
    [SuccessMessage(nameof(Messages.Success_RoomRentalStatusChanged))]
    public async Task<ActionResult<RoomRentalAdminDto>> ChangeStatus(
        int requestId,
        [FromBody] ChangeRoomRentalRequestStatusRequest request,
        CancellationToken cancellationToken
    ) =>
        Ok(await roomRentalAdminService.ChangeStatusAsync(requestId, request, cancellationToken));

    [HttpPost("{requestId:int}/cancel")]
    [SuccessMessage(nameof(Messages.Success_RoomRentalCancelled))]
    public async Task<ActionResult<RoomRentalCancellationResultDto>> Cancel(
        int requestId,
        [FromBody] CancelRoomRentalRequest request,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await roomRentalCancellationService.CancelAsync(
                requestId,
                request.VenueIncident
                    ? RoomRentalCancelKind.Venue
                    : RoomRentalCancelKind.Customer,
                request.Note,
                cancellationToken
            )
        );
}
