using MediatR;
using OrderProcessing.API.Domain.Orders;

namespace OrderProcessing.API.Application.Commands;

public sealed record CreateOrderCommand(
    string CustomerId,
    List<CreateOrderItemCommand> Items,
    decimal TotalAmount,
    string? IdempotencyKey,
    string? CorrelationId) : IRequest<CreateOrderResult>;

public sealed record CreateOrderItemCommand(
    string ProductId,
    int Quantity,
    decimal UnitPrice);

public sealed record CreateOrderResult(
    Guid OrderId,
    OrderStatus Status);

public sealed record OrderCreatedEvent(Guid OrderId);
