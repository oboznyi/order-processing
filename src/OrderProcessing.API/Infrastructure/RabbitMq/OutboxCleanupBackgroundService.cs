using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OrderProcessing.API.Infrastructure.Persistence;

namespace OrderProcessing.API.Infrastructure.RabbitMq;

public sealed class OutboxCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OutboxCleanupOptions> _options;
    private readonly ILogger<OutboxCleanupBackgroundService> _logger;

    public OutboxCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxCleanupOptions> options,
        ILogger<OutboxCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.Value;

            if (!options.Enabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), stoppingToken);
                continue;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

                var thresholdUtc = DateTime.UtcNow.AddHours(-options.RetentionHours);
                var deletedRows = await outboxRepository.DeletePublishedOlderThanAsync(
                    thresholdUtc,
                    options.BatchSize,
                    stoppingToken);

                if (deletedRows > 0)
                {
                    _logger.LogInformation(
                        "Outbox cleanup removed {DeletedRows} rows older than {ThresholdUtc}",
                        deletedRows,
                        thresholdUtc);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox cleanup iteration failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), stoppingToken);
        }
    }
}
