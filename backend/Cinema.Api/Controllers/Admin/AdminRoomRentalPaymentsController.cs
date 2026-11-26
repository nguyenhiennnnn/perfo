using Cinema.Api.Filters;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/room-rental-requests/{requestId:int}/payments")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminRoomRentalPaymentsController(
    IRoomRentalPaymentOrderService paymentOrderService,
    IRoomRentalBankTransferService bankTransferService
) : AppControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RoomRentalPaymentsDto>> Get(
        int requestId,
        CancellationToken cancellationToken
    ) => Ok(await paymentOrderService.GetAsync(requestId, cancellationToken));

    [HttpPost("orders")]
    [SuccessMessage(nameof(Messages.Success_RoomRentalOrderCreated))]
    public async Task<ActionResult<RoomRentalPaymentOrderDto>> CreateOrder(
        int requestId,
        [FromBody] CreateRoomRentalPaymentOrderRequest request,
        CancellationToken cancellationToken
    ) =>
        Ok(await paymentOrderService.EnsureOrderAsync(requestId, request.Stage, cancellationToken));

    [HttpPost("bank-transfer")]
    [SuccessMessage(nameof(Messages.Success_RoomRentalPaymentRecorded))]
    public async Task<ActionResult<RoomRentalPaymentOrderDto>> RecordBankTransfer(
        int requestId,
        [FromBody] RecordRoomRentalBankTransferRequest request,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await bankTransferService.RecordAsync(
                requestId,
                request,
                CurrentUserId,
                cancellationToken
            )
        );
}
