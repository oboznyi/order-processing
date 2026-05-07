using OrderProcessing.API.Application.DTOs;
using OrderProcessing.API.Domain.Messaging;
using OrderProcessing.API.Domain.Orders;

namespace OrderProcessing.API.Infrastructure.Persistence;

public interface IOrderRepository
{
    void Add(Order order);

    Task<Order?> GetByIdWithItemsAsync(
        Guid orderId,
        CancellationToken cancellationToken);
}

public interface IOrderReadRepository
{
    Task<OrderResponse?> GetByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken);
}

public interface IInventoryRepository
{
    Task<bool> TryReserveInventoryAsync(
        IReadOnlyCollection<OrderItem> items,
        CancellationToken cancellationToken);
}

public interface IOutboxRepository
{
    void Add(OutboxMessage message);

    Task<List<OutboxMessage>> GetUnpublishedForUpdateAsync(
        int take,
        CancellationToken cancellationToken);

    Task<int> DeletePublishedOlderThanAsync(
        DateTime thresholdUtc,
        int take,
        CancellationToken cancellationToken);
}

public interface IProcessedMessageRepository
{
    Task<bool> ExistsAsync(
        Guid messageId,
        string consumerName,
        CancellationToken cancellationToken);

    void Add(ProcessedMessage message);
}

public interface IIdempotencyRepository
{
    Task<Guid?> GetOrderIdAsync(
        string key,
        CancellationToken cancellationToken);

    void Add(IdempotencyKey key);
}
