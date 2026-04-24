using Shared.Pagination;
using TransactionService.Models;

namespace TransactionService.Interfaces.Services;

public interface ITransactionQueryService
{
    Task<PagedResult<TransactionResponse>> GetAsync(
        TransactionQueryParameters query,
        Guid actingUserId,
        bool canViewAll,
        CancellationToken cancellationToken = default);
    Task<TransactionResponse> GetByIdAsync(Guid transactionId, Guid actingUserId, bool canViewAll, CancellationToken cancellationToken = default);
}
