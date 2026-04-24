using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Constants;
using Shared.Messaging;
using System.Text;
using TransactionService.Configuration;
using TransactionService.Infrastructure.Persistence;

namespace TransactionService.Services.Messaging;

public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> outboxOptions,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private readonly OutboxOptions options = outboxOptions.Value;
    private readonly RabbitMqOptions rabbitMqOptions = rabbitMqOptions.Value;
    private readonly string workerId = Guid.NewGuid().ToString("N");
    private IConnection? connection;
    private IModel? channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedCount = await ProcessBatchAsync(stoppingToken);
                if (processedCount == 0)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(Math.Max(250, options.PollingIntervalMilliseconds)),
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Transaction outbox publisher failed.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    public override void Dispose()
    {
        channel?.Dispose();
        connection?.Dispose();
        base.Dispose();
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransactionServiceDbContext>();
        var messages = await ClaimBatchAsync(dbContext, cancellationToken);
        if (messages.Count == 0)
        {
            return 0;
        }

        var rabbitChannel = EnsureChannel();
        foreach (var message in messages)
        {
            await PublishSingleAsync(dbContext, rabbitChannel, message, cancellationToken);
        }

        return messages.Count;
    }

    private async Task<List<OutboxMessage>> ClaimBatchAsync(TransactionServiceDbContext dbContext, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var leaseUntil = utcNow.AddSeconds(Math.Max(10, options.LockTimeoutSeconds));
        var batchSize = Math.Max(1, options.BatchSize);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null
                && (message.LockedUntilUtc == null || message.LockedUntilUtc < utcNow))
            .OrderBy(message => message.OccurredAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return messages;
        }

        foreach (var message in messages)
        {
            message.LockId = workerId;
            message.LockedUntilUtc = leaseUntil;
            message.LastAttemptAtUtc = utcNow;
            message.AttemptCount += 1;
            message.LastError = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return messages;
    }

    private async Task PublishSingleAsync(
        TransactionServiceDbContext dbContext,
        IModel rabbitChannel,
        OutboxMessage claimedMessage,
        CancellationToken cancellationToken)
    {
        var message = await dbContext.OutboxMessages.FirstOrDefaultAsync(
            item => item.Id == claimedMessage.Id && item.LockId == workerId,
            cancellationToken);
        if (message is null)
        {
            return;
        }

        try
        {
            var body = Encoding.UTF8.GetBytes(message.Payload);
            var properties = rabbitChannel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = message.Id.ToString("N");
            properties.Type = message.MessageType;
            properties.Timestamp = new AmqpTimestamp(new DateTimeOffset(message.OccurredAtUtc).ToUnixTimeSeconds());

            rabbitChannel.BasicPublish(
                exchange: message.Exchange,
                routingKey: message.RoutingKey,
                basicProperties: properties,
                body: body);

            message.ProcessedAtUtc = DateTime.UtcNow;
            message.LockedUntilUtc = null;
            message.LockId = null;
            message.LastError = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            message.LockedUntilUtc = null;
            message.LockId = null;
            message.LastError = Truncate(exception.Message, 2000);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogError(exception, "Failed to publish transaction outbox message {OutboxMessageId}.", message.Id);
        }
    }

    private IModel EnsureChannel()
    {
        if (connection is null || !connection.IsOpen)
        {
            var factory = new ConnectionFactory
            {
                HostName = rabbitMqOptions.HostName,
                Port = rabbitMqOptions.Port,
                UserName = rabbitMqOptions.UserName,
                Password = rabbitMqOptions.Password,
                VirtualHost = rabbitMqOptions.VirtualHost,
                DispatchConsumersAsync = true
            };

            connection = factory.CreateConnection("transaction-service-outbox-publisher");
        }

        if (channel is not null && channel.IsOpen)
        {
            return channel;
        }

        channel = connection.CreateModel();
        DeclareTopology(channel);
        return channel;
    }

    private static void DeclareTopology(IModel model)
    {
        model.ExchangeDeclare(QueueConstants.FileImportResultExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        model.ExchangeDeclare(QueueConstants.FileImportResultDeadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false);

        model.QueueDeclare(
            queue: QueueConstants.FileImportResultQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = QueueConstants.FileImportResultDeadLetterExchange
            });
        model.QueueBind(QueueConstants.FileImportResultQueue, QueueConstants.FileImportResultExchange, QueueConstants.FileImportResultRoutingKey);

        model.QueueDeclare(
            queue: QueueConstants.FileImportResultDeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
        model.QueueBind(
            QueueConstants.FileImportResultDeadLetterQueue,
            QueueConstants.FileImportResultDeadLetterExchange,
            QueueConstants.FileImportResultRoutingKey);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
