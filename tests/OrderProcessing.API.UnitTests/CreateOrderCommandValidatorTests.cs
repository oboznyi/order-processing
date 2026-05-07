using FluentAssertions;
using OrderProcessing.API.Application.Commands;
using Xunit;

namespace OrderProcessing.API.Tests.Unit;

public sealed class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenTotalAmountDoesNotMatchItems()
    {
        var command = new CreateOrderCommand(
            "customer-1",
            new List<CreateOrderItemCommand>
            {
                new("1", 2, 10m)
            },
            19m,
            null,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("TotalAmount does not match order items sum."));
    }
}
