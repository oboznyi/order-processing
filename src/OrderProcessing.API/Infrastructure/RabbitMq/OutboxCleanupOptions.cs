namespace OrderProcessing.API.Infrastructure.RabbitMq;

public sealed class OutboxCleanupOptions
{
    public bool Enabled { get; init; } = true;
    public int RetentionHours { get; init; } = 24;
    public int BatchSize { get; init; } = 200;
    public int IntervalSeconds { get; init; } = 60;
}
