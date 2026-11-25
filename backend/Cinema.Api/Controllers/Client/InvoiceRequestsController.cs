using Cinema.Api.Filters;
using Cinema.Application.DTOs.Invoices;
using Cinema.Application.Interfaces.Invoices;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/bookings/{orderId:int}/invoice-requests")]
[Authorize]
public class InvoiceRequestsController(IInvoiceRequestService invoiceRequestService)
    : AppControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceRequestDto>>> GetByOrder(
        int orderId,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await invoiceRequestService.GetForCustomerOrderAsync(
                CurrentUserId,
                orderId,
                cancellationToken
            )
        );

    [HttpGet("{requestId:int}/file")]
    public async Task<IActionResult> Download(
        int orderId,
        int requestId,
        CancellationToken cancellationToken
    )
    {
        var file = await invoiceRequestService.DownloadForCustomerAsync(
            CurrentUserId,
            orderId,
            requestId,
            cancellationToken
        );
        return File(file.Content, "application/pdf", file.FileName);
    }

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_InvoiceRequestCreated))]
    public async Task<ActionResult<InvoiceRequestDto>> Create(
        int orderId,
        [FromBody] CreateInvoiceRequestRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await invoiceRequestService.CreateForCustomerAsync(
                CurrentUserId,
                orderId,
                request,
                cancellationToken
            )
        );
}
