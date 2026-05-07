namespace OrderProcessing.API.Domain.Messaging;

public sealed class ProcessedMessage
{
    public Guid Id { get; private set; }
    public Guid MessageId { get; private set; }
    public string ConsumerName { get; private set; } = default!;
    public DateTime ProcessedAtUtc { get; private set; }

    private ProcessedMessage() { }

    public ProcessedMessage(Guid messageId, string consumerName)
    {
        Id = Guid.NewGuid();
        MessageId = messageId;
        ConsumerName = consumerName;
        ProcessedAtUtc = DateTime.UtcNow;
    }
}
