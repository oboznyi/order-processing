namespace OrderProcessing.API.Domain.Orders;

public sealed class Order
{
    public Guid Id { get; private set; }
    public string CustomerId { get; private set; } = default!;
    public List<OrderItem> Items { get; private set; } = new();
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessingStartedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public DateTime? FailedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    private Order() { }

    public Order(string customerId, List<OrderItem> items, decimal totalAmount)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("CustomerId is required.", nameof(customerId));

        if (items.Count == 0)
            throw new ArgumentException("Order must contain at least one item.", nameof(items));

        Id = Guid.NewGuid();
        CustomerId = customerId;
        Items = items;
        TotalAmount = totalAmount;
        Status = OrderStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public bool IsAlreadyProcessed() => Status == OrderStatus.Processed;

    public void MarkAsProcessing()
    {
        if (Status == OrderStatus.Processed)
            return;

        Status = OrderStatus.Processing;
        ProcessingStartedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsProcessed()
    {
        Status = OrderStatus.Processed;
        ProcessedAtUtc = DateTime.UtcNow;
        FailureReason = null;
    }

    public void MarkAsFailed(string reason)
    {
        Status = OrderStatus.Failed;
        FailedAtUtc = DateTime.UtcNow;
        FailureReason = reason;
    }

    public void ApplyDiscount(decimal discountAmount)
    {
        if (discountAmount <= 0)
            return;

        if (discountAmount > TotalAmount)
            throw new ArgumentOutOfRangeException(nameof(discountAmount), "Discount exceeds order total.");

        TotalAmount -= discountAmount;
    }
}
