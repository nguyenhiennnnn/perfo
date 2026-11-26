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
    IRoomRentalTrackingService roomRentalTrackingService,
    IRoomRentalTrackingQuoteService roomRentalTrackingQuoteService
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

    [HttpPost("tracking/quotes")]
    public async Task<ActionResult<RoomRentalTrackingQuotesDto>> TrackQuotes(
        [FromBody] RoomRentalTrackingQuoteRequest request,
        CancellationToken cancellationToken
    ) => Ok(await roomRentalTrackingQuoteService.ListAsync(request.Token, cancellationToken));

    [HttpPost("tracking/quotes/{version:int}/pdf")]
    public async Task<IActionResult> DownloadQuotePdf(
        int version,
        [FromBody] RoomRentalTrackingQuoteRequest request,
        CancellationToken cancellationToken
    )
    {
        var file = await roomRentalTrackingQuoteService.ExportPdfAsync(
            request.Token,
            version,
            cancellationToken
        );
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost("tracking/quotes/{version:int}/accept")]
    [SuccessMessage(nameof(Messages.Success_RoomRentalQuoteAccepted))]
    public async Task<ActionResult<RoomRentalTrackingQuoteDecisionDto>> AcceptQuote(
        int version,
        [FromBody] RoomRentalTrackingQuoteRequest request,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await roomRentalTrackingQuoteService.AcceptAsync(
                request.Token,
                version,
                cancellationToken
            )
        );

    [HttpPost("tracking/quotes/{version:int}/decline")]
    [SuccessMessage(nameof(Messages.Success_RoomRentalQuoteDeclined))]
    public async Task<ActionResult<RoomRentalTrackingQuoteDecisionDto>> DeclineQuote(
        int version,
        [FromBody] RoomRentalTrackingQuoteDeclineRequest request,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await roomRentalTrackingQuoteService.DeclineAsync(
                request.Token,
                version,
                request.Reason,
                cancellationToken
            )
        );
}
