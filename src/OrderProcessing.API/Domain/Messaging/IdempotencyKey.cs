namespace OrderProcessing.API.Domain.Messaging;

public sealed class IdempotencyKey
{
    public Guid Id { get; private set; }
    public string Key { get; private set; } = default!;
    public Guid OrderId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private IdempotencyKey() { }

    public IdempotencyKey(string key, Guid orderId)
    {
        Id = Guid.NewGuid();
        Key = key;
        OrderId = orderId;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
