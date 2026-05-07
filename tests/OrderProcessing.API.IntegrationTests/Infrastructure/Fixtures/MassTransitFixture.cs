using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace OrderProcessing.API.IntegrationTests.Infrastructure.Fixtures;

public sealed class MassTransitFixture
{
    public InMemoryTestHarness GetHarness()
    {
        return new InMemoryTestHarness();
    }
}

public static class InMemoryTestHarnessExtensions
{
    public static async Task Send<T>(this InMemoryTestHarness harness, T message, Guid correlationId)
        where T : class
    {
        await harness.InputQueueSendEndpoint.Send(message, context =>
        {
            context.CorrelationId = correlationId;
        });

        using var source = new CancellationTokenSource(TimeSpan.FromSeconds(4));

        await harness.Consumed.Any<T>(c => c.Context.CorrelationId == correlationId, source.Token);
    }

    public static ConsumerTestHarness<T> Consumer<T>(
        this InMemoryTestHarness harness,
        IServiceProvider serviceProvider,
        params object[] parameters)
        where T : class, IConsumer
    {
        return harness.Consumer(() => ActivatorUtilities.CreateInstance<T>(serviceProvider, parameters));
    }
}
