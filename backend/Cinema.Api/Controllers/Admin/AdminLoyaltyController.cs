using Cinema.Api.Filters;
using Cinema.Application.DTOs.Loyalty;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/loyalty")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminLoyaltyController(ILoyaltyAdminService loyaltyAdminService) : AppControllerBase
{
    [HttpGet("customers")]
    public async Task<IActionResult> SearchCustomers(
        [FromQuery] string? q,
        CancellationToken cancellationToken
    ) => Ok(await loyaltyAdminService.SearchCustomersAsync(q, cancellationToken));

    [HttpGet("customers/{userId:int}")]
    public async Task<IActionResult> GetCustomer(int userId, CancellationToken cancellationToken) =>
        Ok(await loyaltyAdminService.GetCustomerAsync(userId, cancellationToken));

    [HttpGet("customers/{userId:int}/transactions")]
    public async Task<IActionResult> GetTransactions(
        int userId,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) => Ok(await loyaltyAdminService.GetTransactionsAsync(userId, page, pageSize, cancellationToken));

    [HttpPost("customers/{userId:int}/adjustments")]
    [SuccessMessage(nameof(Messages.Success_LoyaltyPointsAdjusted))]
    public async Task<IActionResult> Adjust(
        int userId,
        [FromBody] AdjustLoyaltyPointsRequest request,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await loyaltyAdminService.AdjustAsync(
                userId,
                CurrentUserId,
                request,
                cancellationToken
            )
        );
}
