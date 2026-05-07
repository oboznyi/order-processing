using OrderProcessing.API.Domain.Orders;

namespace OrderProcessing.API.Application.DTOs;

public sealed record OrderResponse(
    Guid Id,
    string CustomerId,
    decimal TotalAmount,
    OrderStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ProcessingStartedAtUtc,
    DateTime? ProcessedAtUtc,
    DateTime? FailedAtUtc,
    string? FailureReason,
    List<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    string ProductId,
    int Quantity,
    decimal UnitPrice);
