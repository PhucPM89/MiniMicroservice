namespace Shared.Constants;

public static class QueueConstants
{
    public const string FileImportExchange = "file.import.exchange";
    public const string FileImportRoutingKey = "file-import.requested";
    public const string FileImportQueue = "transactions.file-import.requested";
    public const string FileImportDeadLetterExchange = "file.import.dlx";
    public const string FileImportDeadLetterQueue = "transactions.file-import.requested.dlq";
    public const string TransactionImportConsumer = "TransactionService.FileImportConsumer";

    public const string FileImportResultExchange = "file.import.result.exchange";
    public const string FileImportResultRoutingKey = "file-import.resulted";
    public const string FileImportResultQueue = "files.file-import.resulted";
    public const string FileImportResultDeadLetterExchange = "file.import.result.dlx";
    public const string FileImportResultDeadLetterQueue = "files.file-import.resulted.dlq";
    public const string FileImportResultConsumer = "FileService.FileImportResultConsumer";
}
