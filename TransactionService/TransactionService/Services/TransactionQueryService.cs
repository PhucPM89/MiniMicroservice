using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using Shared.Pagination;
using TransactionService.Infrastructure.Persistence;
using TransactionService.Models;

namespace TransactionService.Services;

public sealed class TransactionQueryService(TransactionServiceDbContext dbContext) : Interfaces.Services.ITransactionQueryService
{
    public async Task<PagedResult<TransactionResponse>> GetAsync(
        TransactionQueryParameters query,
        Guid actingUserId,
        bool canViewAll,
        CancellationToken cancellationToken = default)
    {
        var pageSize = NormalizePageSize(query.PageSize);
        var cursor = CursorTokenSerializer.Decode(query.Cursor);

        var transactionsQuery = dbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.ImportBatch)
            .AsQueryable();

        if (!canViewAll)
        {
            transactionsQuery = transactionsQuery.Where(
                transaction => transaction.ImportBatch.UploadedByUserId == actingUserId);
        }

        if (query.FileId.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(transaction => transaction.ImportBatch.FileId == query.FileId.Value);
        }

        if (query.ImportBatchId.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(transaction => transaction.ImportBatchId == query.ImportBatchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.TransactionId))
        {
            var transactionId = query.TransactionId.Trim();
            transactionsQuery = transactionsQuery.Where(transaction => transaction.TransactionId.Contains(transactionId));
        }

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            var type = query.Type.Trim();
            transactionsQuery = transactionsQuery.Where(transaction => transaction.Type == type);
        }

        if (query.MinAmount.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(transaction => transaction.Amount >= query.MinAmount.Value);
        }

        if (query.MaxAmount.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(transaction => transaction.Amount <= query.MaxAmount.Value);
        }

        if (cursor is not null)
        {
            var timestampUtc = cursor.TimestampUtc;
            var lastId = cursor.LastId;

            transactionsQuery = transactionsQuery.Where(transaction =>
                transaction.CreatedAtUtc < timestampUtc
                || (transaction.CreatedAtUtc == timestampUtc && transaction.Id.CompareTo(lastId) < 0));
        }

        var rows = await transactionsQuery
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .ThenByDescending(transaction => transaction.Id)
            .Take(pageSize + 1)
            .Select(transaction => new TransactionPageRow(
                transaction.Id,
                transaction.CreatedAtUtc,
                new TransactionResponse
                {
                    Id = transaction.Id,
                    ImportBatchId = transaction.ImportBatchId,
                    FileId = transaction.ImportBatch.FileId,
                    FileName = transaction.ImportBatch.FileName,
                    TransactionId = transaction.TransactionId,
                    Amount = transaction.Amount,
                    Type = transaction.Type,
                    Description = transaction.Description,
                    RawLineNumber = transaction.RawLineNumber,
                    CreatedAtUtc = transaction.CreatedAtUtc
                }))
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > pageSize;
        var pageRows = hasMore ? rows.Take(pageSize).ToList() : rows;
        var items = pageRows.Select(row => row.Response).ToList();

        return new PagedResult<TransactionResponse>
        {
            Items = items,
            PageSize = pageSize,
            HasMore = hasMore,
            NextCursor = hasMore ? CreateNextCursor(pageRows) : null
        };
    }

    public async Task<TransactionResponse> GetByIdAsync(
        Guid transactionId,
        Guid actingUserId,
        bool canViewAll,
        CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Transactions
            .AsNoTracking()
            .Include(item => item.ImportBatch)
            .Where(item => item.Id == transactionId)
            .Select(item => new TransactionResponse
            {
                Id = item.Id,
                ImportBatchId = item.ImportBatchId,
                FileId = item.ImportBatch.FileId,
                FileName = item.ImportBatch.FileName,
                TransactionId = item.TransactionId,
                Amount = item.Amount,
                Type = item.Type,
                Description = item.Description,
                RawLineNumber = item.RawLineNumber,
                CreatedAtUtc = item.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Transaction '{transactionId}' was not found.");

        if (!canViewAll && transaction.FileId != Guid.Empty)
        {
            var isOwned = await dbContext.ImportBatches
                .AsNoTracking()
                .AnyAsync(
                    batch => batch.Id == transaction.ImportBatchId && batch.UploadedByUserId == actingUserId,
                    cancellationToken);

            if (!isOwned)
            {
                throw new ForbiddenException("You do not have permission to view this transaction.");
            }
        }

        return transaction;
    }

    private static int NormalizePageSize(int pageSize)
    {
        return pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
    }

    private static string CreateNextCursor(IReadOnlyList<TransactionPageRow> items)
    {
        var lastItem = items[^1];

        return CursorTokenSerializer.Encode(new TimestampCursor
        {
            TimestampUtc = lastItem.CreatedAtUtc,
            LastId = lastItem.Id
        });
    }

    private sealed record TransactionPageRow(Guid Id, DateTime CreatedAtUtc, TransactionResponse Response);
}
