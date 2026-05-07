namespace OrderProcessing.API.Domain.Messaging;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public string? CorrelationId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public int RetryCount { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string type, string payload, string? correlationId)
    {
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        CorrelationId = correlationId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsPublished()
    {
        PublishedAtUtc = DateTime.UtcNow;
        Error = null;
    }

    public void MarkAsFailed(string error)
    {
        RetryCount++;
        Error = error;
    }
}
