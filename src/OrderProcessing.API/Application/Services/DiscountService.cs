using OrderProcessing.API.Domain.Orders;

namespace OrderProcessing.API.Application.Services;

public sealed class DiscountService : IDiscountService
{
    public decimal CalculateDiscount(Order order)
    {
        var discountRate = order.TotalAmount switch
        {
            >= 100m => 0.10m,
            >= 50m => 0.05m,
            _ => 0m
        };

        return decimal.Round(order.TotalAmount * discountRate, 2, MidpointRounding.AwayFromZero);
    }
}
