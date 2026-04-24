namespace TransactionService.Configuration;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollingIntervalMilliseconds { get; set; } = 1000;
    public int BatchSize { get; set; } = 20;
    public int LockTimeoutSeconds { get; set; } = 30;
}
