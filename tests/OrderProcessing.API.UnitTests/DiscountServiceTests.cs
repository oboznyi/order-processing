using FluentAssertions;
using OrderProcessing.API.Application.Services;
using OrderProcessing.API.Domain.Orders;
using Xunit;

namespace OrderProcessing.API.UnitTests;

public sealed class DiscountServiceTests
{
    private readonly DiscountService _discountService = new();

    [Fact]
    public void CalculateDiscount_ShouldReturnTenPercent_WhenTotalIsHundredOrMore()
    {
        var order = new Order(
            "customer-10",
            new List<OrderItem>
            {
                new("1", 2, 60m)
            },
            120m);

        var discount = _discountService.CalculateDiscount(order);

        discount.Should().Be(12m);
    }
}
