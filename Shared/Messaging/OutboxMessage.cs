namespace Shared.Messaging;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Exchange { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public string MessageKey { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? LockId { get; set; }
    public string? LastError { get; set; }
}
