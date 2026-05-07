namespace OrderProcessing.API.Infrastructure.Metrics;

public interface IOrderMetrics
{
    int IncrementProcessedOrders();
    int IncrementFailedOrders();
}
