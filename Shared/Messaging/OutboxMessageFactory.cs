using System.Text.Json;

namespace Shared.Messaging;

public static class OutboxMessageFactory
{
    public static OutboxMessage Create<T>(string exchange, string routingKey, string messageKey, T message)
    {
        return new OutboxMessage
        {
            Exchange = exchange,
            RoutingKey = routingKey,
            MessageKey = messageKey,
            MessageType = typeof(T).FullName ?? typeof(T).Name,
            Payload = JsonSerializer.Serialize(message, MessagingJsonDefaults.Default),
            OccurredAtUtc = DateTime.UtcNow
        };
    }
}
