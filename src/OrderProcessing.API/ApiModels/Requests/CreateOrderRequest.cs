namespace OrderProcessing.API.ApiModels.Requests;

public sealed record CreateOrderRequest(
    string CustomerId,
    List<CreateOrderItemRequest> Items,
    decimal TotalAmount);

public sealed record CreateOrderItemRequest(
    string ProductId,
    int Quantity,
    decimal UnitPrice);
