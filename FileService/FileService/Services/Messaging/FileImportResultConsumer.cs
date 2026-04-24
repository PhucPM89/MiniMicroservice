using FileService.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Constants;
using Shared.Messaging;
using System.Text;
using System.Text.Json;

namespace FileService.Services.Messaging;

public sealed class FileImportResultConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    ILogger<FileImportResultConsumer> logger) : BackgroundService
{
    private readonly RabbitMqOptions rabbitMqOptions = rabbitMqOptions.Value;
    private IConnection? connection;
    private IModel? channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitChannel = EnsureChannel();
        rabbitChannel.BasicQos(0, 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(rabbitChannel);
        consumer.Received += async (_, eventArgs) =>
        {
            var outcome = await HandleMessageAsync(eventArgs, stoppingToken);
            if (!rabbitChannel.IsOpen)
            {
                return;
            }

            if (outcome == ConsumeOutcome.Ack)
            {
                rabbitChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            else
            {
                rabbitChannel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };

        rabbitChannel.BasicConsume(
            queue: QueueConstants.FileImportResultQueue,
            autoAck: false,
            consumer: consumer);

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        stoppingToken.Register(() => completion.TrySetResult());
        return completion.Task;
    }

    public override void Dispose()
    {
        channel?.Dispose();
        connection?.Dispose();
        base.Dispose();
    }

    private async Task<ConsumeOutcome> HandleMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        var messageId = eventArgs.BasicProperties.MessageId;
        if (string.IsNullOrWhiteSpace(messageId))
        {
            logger.LogError("Received file import result message without MessageId. Sending to dead letter queue.");
            return ConsumeOutcome.DeadLetter;
        }

        var claimResult = await ClaimInboxMessageAsync(messageId, eventArgs, payload, cancellationToken);
        if (claimResult == InboxClaimResult.AlreadyProcessed)
        {
            return ConsumeOutcome.Ack;
        }

        try
        {
            var message = JsonSerializer.Deserialize<FileImportResultMessage>(payload, MessagingJsonDefaults.Default)
                ?? throw new JsonException("The file import result payload is invalid.");

            using var processScope = scopeFactory.CreateScope();
            var resultService = processScope.ServiceProvider.GetRequiredService<FileImportResultService>();
            await resultService.ApplyAsync(message, cancellationToken);

            await MarkInboxProcessedAsync(messageId, cancellationToken);
            return ConsumeOutcome.Ack;
        }
        catch (Exception exception)
        {
            await MarkInboxFailedAsync(messageId, exception, cancellationToken);
            logger.LogError(exception, "Failed to consume file import result message {MessageId}.", messageId);
            return ConsumeOutcome.DeadLetter;
        }
    }

    private async Task<InboxClaimResult> ClaimInboxMessageAsync(
        string messageId,
        BasicDeliverEventArgs eventArgs,
        string payload,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.FileServiceDbContext>();
        var inboxMessage = await dbContext.InboxMessages.FirstOrDefaultAsync(
            item => item.MessageId == messageId && item.Consumer == QueueConstants.FileImportResultConsumer,
            cancellationToken);

        if (inboxMessage is not null && inboxMessage.ProcessedAtUtc.HasValue)
        {
            return InboxClaimResult.AlreadyProcessed;
        }

        inboxMessage ??= new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            Consumer = QueueConstants.FileImportResultConsumer,
            ReceivedAtUtc = DateTime.UtcNow
        };

        inboxMessage.Exchange = eventArgs.Exchange;
        inboxMessage.RoutingKey = eventArgs.RoutingKey;
        inboxMessage.MessageType = eventArgs.BasicProperties.Type ?? typeof(FileImportResultMessage).FullName ?? nameof(FileImportResultMessage);
        inboxMessage.Payload = payload;
        inboxMessage.LastAttemptAtUtc = DateTime.UtcNow;
        inboxMessage.AttemptCount += 1;
        inboxMessage.LockId = messageId;
        inboxMessage.LockedUntilUtc = DateTime.UtcNow.AddMinutes(1);
        inboxMessage.LastError = null;

        if (dbContext.Entry(inboxMessage).State == EntityState.Detached)
        {
            await dbContext.InboxMessages.AddAsync(inboxMessage, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return InboxClaimResult.Claimed;
    }

    private async Task MarkInboxProcessedAsync(string messageId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.FileServiceDbContext>();
        var inboxMessage = await dbContext.InboxMessages.FirstOrDefaultAsync(
            item => item.MessageId == messageId && item.Consumer == QueueConstants.FileImportResultConsumer,
            cancellationToken);
        if (inboxMessage is null)
        {
            return;
        }

        inboxMessage.ProcessedAtUtc = DateTime.UtcNow;
        inboxMessage.LockId = null;
        inboxMessage.LockedUntilUtc = null;
        inboxMessage.LastError = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkInboxFailedAsync(string messageId, Exception exception, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.FileServiceDbContext>();
            var inboxMessage = await dbContext.InboxMessages.FirstOrDefaultAsync(
                item => item.MessageId == messageId && item.Consumer == QueueConstants.FileImportResultConsumer,
                cancellationToken);
            if (inboxMessage is null)
            {
                return;
            }

            inboxMessage.LockId = null;
            inboxMessage.LockedUntilUtc = null;
            inboxMessage.LastError = Truncate(exception.Message, 2000);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception markException)
        {
            logger.LogError(markException, "Failed to persist file inbox failure state for message {MessageId}.", messageId);
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

            connection = factory.CreateConnection("file-service-import-result-consumer");
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

    private enum ConsumeOutcome
    {
        Ack,
        DeadLetter
    }

    private enum InboxClaimResult
    {
        Claimed,
        AlreadyProcessed
    }
}
