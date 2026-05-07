namespace OrderProcessing.API.Infrastructure.RabbitMq;

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string VirtualHost { get; init; } = "/";
    public string QueueName { get; init; } = "order-processing-queue";
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
}
