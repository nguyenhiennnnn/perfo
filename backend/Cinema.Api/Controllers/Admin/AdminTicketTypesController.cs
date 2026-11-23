using Cinema.Api.Filters;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/ticket-types")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminTicketTypesController(ITicketTypeService ticketTypeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await ticketTypeService.GetAdminAsync(
                page,
                pageSize,
                new TicketTypeListFilter(search, isActive),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await ticketTypeService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_TicketTypeCreated))]
    public async Task<IActionResult> Create(
        [FromBody] UpsertTicketTypeRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await ticketTypeService.CreateAsync(request, cancellationToken)
        );

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_TicketTypeUpdated))]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertTicketTypeRequest request,
        CancellationToken cancellationToken
    ) => Ok(await ticketTypeService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_TicketTypeDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await ticketTypeService.DeleteAsync(id, cancellationToken));
}
