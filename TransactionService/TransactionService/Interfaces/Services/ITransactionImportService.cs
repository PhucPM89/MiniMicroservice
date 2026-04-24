using Shared.Messaging;

namespace TransactionService.Interfaces.Services;

public interface ITransactionImportService
{
    Task ProcessAsync(FileImportRequestedMessage message, CancellationToken cancellationToken = default);
}
