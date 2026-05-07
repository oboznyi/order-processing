using System.Text.Json;
using MassTransit;
using OrderProcessing.API.Application.Commands;
using OrderProcessing.API.Infrastructure.Persistence;

namespace OrderProcessing.API.Infrastructure.RabbitMq;

public sealed class OutboxPublisherBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherBackgroundService> _logger;

    public OutboxPublisherBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisherBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox publisher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    var messages = await outboxRepository.GetUnpublishedForUpdateAsync(20, ct);

                    foreach (var message in messages)
                    {
                        try
                        {
                            if (message.Type == nameof(OrderCreatedEvent))
                            {
                                var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Payload);

                                if (@event is null)
                                    throw new InvalidOperationException("Invalid outbox payload.");

                                await publishEndpoint.Publish(@event, publishContext =>
                                {
                                    if (!string.IsNullOrWhiteSpace(message.CorrelationId))
                                        publishContext.Headers.Set("X-Correlation-Id", message.CorrelationId);
                                }, ct);
                            }

                            message.MarkAsPublished();

                            _logger.LogInformation(
                                "Outbox message {OutboxMessageId} published",
                                message.Id);
                        }
                        catch (Exception ex)
                        {
                            message.MarkAsFailed(ex.Message);

                            _logger.LogError(
                                ex,
                                "Failed to publish outbox message {OutboxMessageId}",
                                message.Id);
                        }
                    }

                    await unitOfWork.SaveChangesAsync(ct);
                }, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox publisher iteration failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
