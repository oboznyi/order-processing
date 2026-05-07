using FluentValidation;

namespace OrderProcessing.API.Application.Commands;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Items)
            .NotEmpty();

        RuleForEach(x => x.Items)
            .SetValidator(new CreateOrderItemCommandValidator());

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0);

        RuleFor(x => x)
            .Must(HaveCorrectTotal)
            .WithMessage("TotalAmount does not match order items sum.");
    }

    private static bool HaveCorrectTotal(CreateOrderCommand command)
    {
        if (command.Items.Count == 0)
            return false;

        var sum = command.Items.Sum(x => x.Quantity * x.UnitPrice);
        return sum == command.TotalAmount;
    }
}

public sealed class CreateOrderItemCommandValidator : AbstractValidator<CreateOrderItemCommand>
{
    public CreateOrderItemCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThan(0);
    }
}
