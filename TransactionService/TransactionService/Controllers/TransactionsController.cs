using Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Pagination;
using TransactionService.Common;
using TransactionService.Interfaces.Services;
using TransactionService.Models;

namespace TransactionService.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public sealed class TransactionsController(ITransactionQueryService transactionQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionResponse>>> GetTransactions(
        [FromQuery] TransactionQueryParameters query,
        CancellationToken cancellationToken)
    {
        var actingUserId = User.GetRequiredUserId();
        var canViewAll = User.IsInRole(RoleConstants.Admin);
        var response = await transactionQueryService.GetAsync(query, actingUserId, canViewAll, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{transactionId:guid}")]
    public async Task<ActionResult<TransactionResponse>> GetById(Guid transactionId, CancellationToken cancellationToken)
    {
        var actingUserId = User.GetRequiredUserId();
        var canViewAll = User.IsInRole(RoleConstants.Admin);
        var response = await transactionQueryService.GetByIdAsync(transactionId, actingUserId, canViewAll, cancellationToken);
        return Ok(response);
    }
}
