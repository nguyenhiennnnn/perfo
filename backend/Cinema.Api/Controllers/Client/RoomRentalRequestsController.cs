using Cinema.Api.Filters;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/room-rental-requests")]
[AllowAnonymous]
public class RoomRentalRequestsController(
    IRoomRentalRequestService roomRentalRequestService,
    IRoomRentalTrackingService roomRentalTrackingService
) : AppControllerBase
{
    public const string RateLimitForm = "room-rental-request";

    [HttpPost]
    [PublicFormRateLimit(RateLimitForm)]
    [SuccessMessage(nameof(Messages.Success_RoomRentalRequestCreated))]
    public async Task<ActionResult<RoomRentalRequestReceiptDto>> Create(
        [FromBody] CreateRoomRentalRequestRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await roomRentalRequestService.CreatePublicAsync(request, cancellationToken)
        );

    [HttpPost("tracking")]
    public async Task<ActionResult<RoomRentalTrackingDto>> Track(
        [FromBody] RoomRentalTrackingRequest request,
        CancellationToken cancellationToken
    ) => Ok(await roomRentalTrackingService.GetByTokenAsync(request.Token, cancellationToken));
}
