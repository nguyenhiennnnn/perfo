using Cinema.Api.Filters;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/room-rental-requests/{requestId:int}/quotes")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminRoomRentalQuoteVersionsController(
    IRoomRentalQuoteVersionService versionService,
    IRoomRentalQuotePdfService pdfService,
    IRoomRentalQuoteDeliveryService deliveryService
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int requestId, CancellationToken cancellationToken) =>
        Ok(await versionService.ListAsync(requestId, cancellationToken));

    [HttpGet("{version:int}")]
    public async Task<IActionResult> Get(
        int requestId,
        int version,
        CancellationToken cancellationToken
    ) => Ok(await versionService.GetAsync(requestId, version, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_RoomRentalQuoteVersionCreated))]
    public async Task<IActionResult> Create(
        int requestId,
        [FromBody] UpsertRoomRentalQuoteVersionRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await versionService.CreateAsync(requestId, request, cancellationToken)
        );

    [HttpGet("{version:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(
        int requestId,
        int version,
        CancellationToken cancellationToken
    )
    {
        var file = await pdfService.ExportAsync(requestId, version, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost("{version:int}/send")]
    [SuccessMessage(nameof(Messages.Success_RoomRentalQuoteSent))]
    public async Task<IActionResult> Send(
        int requestId,
        int version,
        CancellationToken cancellationToken
    ) => Ok(await deliveryService.SendAsync(requestId, version, cancellationToken));
}
