using OrderProcessing.API.Domain.Orders;

namespace OrderProcessing.API.Application.Services;

public interface IDiscountService
{
    decimal CalculateDiscount(Order order);
}
