using System.Threading;
using Prometheus;

namespace OrderProcessing.API.Infrastructure.Metrics;

public sealed class OrderMetrics : IOrderMetrics
{
    private static readonly Counter ProcessedOrdersCounter =
        Prometheus.Metrics.CreateCounter("orders_processed_total", "Total number of processed orders.");

    private static readonly Counter FailedOrdersCounter =
        Prometheus.Metrics.CreateCounter("orders_failed_total", "Total number of failed orders.");

    private int _processedOrders;
    private int _failedOrders;

    public int IncrementProcessedOrders()
    {
        ProcessedOrdersCounter.Inc();
        return Interlocked.Increment(ref _processedOrders);
    }

    public int IncrementFailedOrders()
    {
        FailedOrdersCounter.Inc();
        return Interlocked.Increment(ref _failedOrders);
    }
}
