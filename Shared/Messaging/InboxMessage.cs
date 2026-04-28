namespace Shared.Messaging;

public sealed class InboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MessageId { get; set; } = string.Empty;
    public string Consumer { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? LockId { get; set; }
    public string? LastError { get; set; }
}
