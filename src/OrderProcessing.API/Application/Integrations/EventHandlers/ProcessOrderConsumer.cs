using MassTransit;
using OrderProcessing.API.Application.Commands;
using OrderProcessing.API.Application.Extensions;
using OrderProcessing.API.Application.Services;
using OrderProcessing.API.Domain.Messaging;
using OrderProcessing.API.Infrastructure.Metrics;
using OrderProcessing.API.Infrastructure.Persistence;

namespace OrderProcessing.API.Application.Integrations.EventHandlers;

public sealed class ProcessOrderConsumer : IConsumer<OrderCreatedEvent>
{
    private const string ConsumerName = nameof(ProcessOrderConsumer);

    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IProcessedMessageRepository _processedMessageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDiscountService _discountService;
    private readonly IOrderMetrics _orderMetrics;
    private readonly ILogger<ProcessOrderConsumer> _logger;

    public ProcessOrderConsumer(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IProcessedMessageRepository processedMessageRepository,
        IUnitOfWork unitOfWork,
        IDiscountService discountService,
        IOrderMetrics orderMetrics,
        ILogger<ProcessOrderConsumer> logger)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _processedMessageRepository = processedMessageRepository;
        _unitOfWork = unitOfWork;
        _discountService = discountService;
        _orderMetrics = orderMetrics;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var messageId = context.MessageId ?? NewId.NextGuid();
        var correlationId = context.Headers.Get<string>("X-Correlation-Id");

        var alreadyProcessed = await _processedMessageRepository.ExistsAsync(
            messageId,
            ConsumerName,
            context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Message {MessageId} already processed by {ConsumerName}. CorrelationId: {CorrelationId}",
                messageId,
                ConsumerName,
                correlationId);

            return;
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await _orderRepository.GetByIdWithItemsAsync(
                context.Message.OrderId,
                ct);

            if (order is null)
            {
                _logger.LogWarning(
                    "Order {OrderId} was not found. CorrelationId: {CorrelationId}",
                    context.Message.OrderId,
                    correlationId);

                return;
            }

            if (order.IsAlreadyProcessed())
            {
                _processedMessageRepository.Add(new ProcessedMessage(messageId, ConsumerName));
                await _unitOfWork.SaveChangesAsync(ct);
                return;
            }

            order.MarkAsProcessing();
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogOrderProcessingStarted(order, correlationId);

            await Task.Delay(TimeSpan.FromSeconds(1), ct);

            var inventoryReserved = await _inventoryRepository.TryReserveInventoryAsync(
                order.Items,
                ct);

            if (!inventoryReserved)
            {
                const string reason = "Inventory is not available";

                order.MarkAsFailed(reason);
                _processedMessageRepository.Add(new ProcessedMessage(messageId, ConsumerName));

                await _unitOfWork.SaveChangesAsync(ct);

                var failedTotal = _orderMetrics.IncrementFailedOrders();
                _logger.LogOrderFailed(order, reason, correlationId);
                _logger.LogInformation(
                    "Orders failed total: {FailedOrdersTotal}. CorrelationId: {CorrelationId}",
                    failedTotal,
                    correlationId);

                return;
            }

            var discountAmount = _discountService.CalculateDiscount(order);
            order.ApplyDiscount(discountAmount);

            if (discountAmount > 0)
            {
                _logger.LogInformation(
                    "Discount {DiscountAmount} applied for order {OrderId}. CorrelationId: {CorrelationId}",
                    discountAmount,
                    order.Id,
                    correlationId);
            }

            order.MarkAsProcessed();
            _processedMessageRepository.Add(new ProcessedMessage(messageId, ConsumerName));

            await _unitOfWork.SaveChangesAsync(ct);

            var processedTotal = _orderMetrics.IncrementProcessedOrders();
            _logger.LogOrderProcessed(order, correlationId);
            _logger.LogInformation(
                "Orders processed total: {ProcessedOrdersTotal}. CorrelationId: {CorrelationId}",
                processedTotal,
                correlationId);
        }, context.CancellationToken);
    }
}
