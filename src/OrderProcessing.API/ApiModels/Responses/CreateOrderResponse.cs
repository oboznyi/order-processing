using OrderProcessing.API.Domain.Orders;

namespace OrderProcessing.API.ApiModels.Responses;
public sealed record CreateOrderResponse(
    Guid OrderId,
    OrderStatus Status);