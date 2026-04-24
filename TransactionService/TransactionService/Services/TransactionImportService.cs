using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Models.Services;
using Shared.Exceptions;
using Shared.Constants;
using Shared.Messaging;
using System.Globalization;
using System.Text;
using TransactionService.Entities;
using TransactionService.Infrastructure.Persistence;
using TransactionService.Interfaces.Services;

namespace TransactionService.Services;

public sealed class TransactionImportService(
    TransactionServiceDbContext dbContext,
    ILogger<TransactionImportService> logger) : ITransactionImportService
{
    private const int PersistenceBatchSize = 1000;
    private static readonly string[] ExpectedHeaders = ["TransactionId", "Amount", "Type", "Description"];

    public async Task ProcessAsync(FileImportRequestedMessage message, CancellationToken cancellationToken = default)
    {
        ValidateMessage(message);

        var existingBatch = await dbContext.ImportBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(batch => batch.FileId == message.FileId, cancellationToken);
        if (existingBatch is not null)
        {
            logger.LogInformation("Skipping duplicate import request for file {FileId}.", message.FileId);
            return;
        }

        var batch = new ImportBatch
        {
            Id = Guid.NewGuid(),
            FileId = message.FileId,
            UploadedByUserId = message.UploadedByUserId,
            FileName = message.OriginalFileName.Trim(),
            CorrelationId = message.CorrelationId,
            Status = ImportBatchStatuses.Processing,
            StartedAtUtc = DateTime.UtcNow
        };

        await dbContext.ImportBatches.AddAsync(batch, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await ProcessFileAsync(batch, message, cancellationToken);
        }
        catch (ValidationException exception)
        {
            logger.LogWarning(exception, "File import failed validation for file {FileId}.", message.FileId);
            batch.Status = ImportBatchStatuses.Failed;
            batch.ErrorMessage = exception.Message;
            batch.CompletedAtUtc = DateTime.UtcNow;
            dbContext.OutboxMessages.Add(CreateResultOutboxMessage(batch));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessFileAsync(ImportBatch batch, FileImportRequestedMessage message, CancellationToken cancellationToken)
    {
        var transactions = new List<TransactionRecord>(PersistenceBatchSize);
        var errors = new List<TransactionErrorRecord>(PersistenceBatchSize);
        var seenTransactionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var successfulRecords = 0;
        var failedRecords = 0;

        using var parser = CreateParser(message);
        if (parser.EndOfData)
        {
            throw new ValidationException(["The CSV file is empty."], "File import failed.");
        }

        var headers = parser.ReadFields();
        if (!HeadersMatch(headers))
        {
            throw new ValidationException(
                [$"The CSV header is invalid. Expected: {string.Join(", ", ExpectedHeaders)}."],
                "File import failed.");
        }

        var lineNumber = 1;
        while (!parser.EndOfData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineNumber++;

            string[]? fields;
            try
            {
                fields = parser.ReadFields();
            }
            catch (MalformedLineException exception)
            {
                errors.Add(CreateError(batch.Id, lineNumber, null, exception.Message));
                failedRecords++;
                await FlushErrorsAsync(errors, cancellationToken);
                continue;
            }

            if (fields is null || fields.Length != ExpectedHeaders.Length)
            {
                errors.Add(CreateError(
                    batch.Id,
                    lineNumber,
                    fields is null ? null : string.Join(",", fields),
                    $"Expected {ExpectedHeaders.Length} columns but found {fields?.Length ?? 0}."));
                failedRecords++;
                await FlushErrorsAsync(errors, cancellationToken);
                continue;
            }

            var transactionId = fields[0].Trim();
            var errorMessages = ValidateRow(fields, seenTransactionIds, transactionId);
            if (errorMessages.Count > 0)
            {
                errors.Add(CreateError(batch.Id, lineNumber, string.Join(",", fields), string.Join(" ", errorMessages)));
                failedRecords++;
                await FlushErrorsAsync(errors, cancellationToken);
                continue;
            }

            seenTransactionIds.Add(transactionId);
            transactions.Add(new TransactionRecord
            {
                Id = Guid.NewGuid(),
                ImportBatchId = batch.Id,
                TransactionId = transactionId,
                Amount = decimal.Parse(fields[1], NumberStyles.Number, CultureInfo.InvariantCulture),
                Type = fields[2].Trim(),
                Description = string.IsNullOrWhiteSpace(fields[3]) ? null : fields[3].Trim(),
                RawLineNumber = lineNumber
            });

            successfulRecords++;
            await FlushTransactionsAsync(transactions, cancellationToken);
        }

        await FlushTransactionsAsync(transactions, cancellationToken, force: true);
        await FlushErrorsAsync(errors, cancellationToken, force: true);

        batch.TotalRecords = successfulRecords + failedRecords;
        batch.SuccessfulRecords = successfulRecords;
        batch.FailedRecords = failedRecords;
        batch.Status = ImportBatchStatuses.Completed;
        batch.CompletedAtUtc = DateTime.UtcNow;
        batch.ErrorMessage = null;

        dbContext.OutboxMessages.Add(CreateResultOutboxMessage(batch));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task FlushTransactionsAsync(
        List<TransactionRecord> transactions,
        CancellationToken cancellationToken,
        bool force = false)
    {
        if (transactions.Count == 0 || (!force && transactions.Count < PersistenceBatchSize))
        {
            return;
        }

        await dbContext.Transactions.AddRangeAsync(transactions, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        DetachPersistedRows();
        transactions.Clear();
    }

    private async Task FlushErrorsAsync(
        List<TransactionErrorRecord> errors,
        CancellationToken cancellationToken,
        bool force = false)
    {
        if (errors.Count == 0 || (!force && errors.Count < PersistenceBatchSize))
        {
            return;
        }

        await dbContext.TransactionErrors.AddRangeAsync(errors, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        DetachPersistedRows();
        errors.Clear();
    }

    private void DetachPersistedRows()
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<TransactionRecord>())
        {
            entry.State = EntityState.Detached;
        }

        foreach (var entry in dbContext.ChangeTracker.Entries<TransactionErrorRecord>())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static OutboxMessage CreateResultOutboxMessage(ImportBatch batch)
    {
        return OutboxMessageFactory.Create(
            QueueConstants.FileImportResultExchange,
            QueueConstants.FileImportResultRoutingKey,
            batch.FileId.ToString("N"),
            new FileImportResultMessage
            {
                ImportBatchId = batch.Id,
                FileId = batch.FileId,
                CorrelationId = batch.CorrelationId,
                Status = batch.Status == ImportBatchStatuses.Failed
                    ? FileImportResultStatuses.Failed
                    : FileImportResultStatuses.Completed,
                TotalRecords = batch.TotalRecords,
                SuccessfulRecords = batch.SuccessfulRecords,
                FailedRecords = batch.FailedRecords,
                ErrorMessage = batch.ErrorMessage,
                CompletedAtUtc = batch.CompletedAtUtc
            });
    }

    private static void ValidateMessage(FileImportRequestedMessage message)
    {
        if (message.FileId == Guid.Empty)
        {
            throw new ValidationException(["FileId is required."], "File import failed.");
        }

        if (string.IsNullOrWhiteSpace(message.OriginalFileName))
        {
            throw new ValidationException(["OriginalFileName is required."], "File import failed.");
        }

        if (string.IsNullOrWhiteSpace(message.FilePath) && string.IsNullOrWhiteSpace(message.FileContentBase64))
        {
            throw new ValidationException(["The file payload is empty."], "File import failed.");
        }
    }

    private static TextFieldParser CreateParser(FileImportRequestedMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.FilePath))
        {
            if (!File.Exists(message.FilePath))
            {
                throw new ValidationException(["The file path does not exist."], "File import failed.");
            }

            var stream = new FileStream(
                message.FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);
            var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var fileParser = new TextFieldParser(reader)
            {
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = true
            };
            fileParser.SetDelimiters(",");
            return fileParser;
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(message.FileContentBase64);
        }
        catch (FormatException)
        {
            throw new ValidationException(["The file payload is not valid Base64 content."], "File import failed.");
        }

        var base64Stream = new MemoryStream(bytes, writable: false);
        var base64Reader = new StreamReader(base64Stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var parser = new TextFieldParser(base64Reader)
        {
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };
        parser.SetDelimiters(",");
        return parser;
    }

    private static bool HeadersMatch(string[]? headers)
    {
        if (headers is null || headers.Length != ExpectedHeaders.Length)
        {
            return false;
        }

        for (var index = 0; index < ExpectedHeaders.Length; index++)
        {
            if (!string.Equals(headers[index], ExpectedHeaders[index], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static List<string> ValidateRow(
        IReadOnlyList<string> fields,
        ISet<string> seenTransactionIds,
        string transactionId)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(transactionId))
        {
            errors.Add("TransactionId is required.");
        }
        else if (seenTransactionIds.Contains(transactionId))
        {
            errors.Add("TransactionId is duplicated within the same file.");
        }

        if (!decimal.TryParse(fields[1], NumberStyles.Number, CultureInfo.InvariantCulture, out _))
        {
            errors.Add("Amount is invalid.");
        }

        if (string.IsNullOrWhiteSpace(fields[2]))
        {
            errors.Add("Type is required.");
        }

        return errors;
    }

    private static TransactionErrorRecord CreateError(Guid importBatchId, int lineNumber, string? rawRecord, string errorMessage)
    {
        return new TransactionErrorRecord
        {
            Id = Guid.NewGuid(),
            ImportBatchId = importBatchId,
            LineNumber = lineNumber,
            RawRecord = rawRecord,
            ErrorMessage = errorMessage
        };
    }
}
