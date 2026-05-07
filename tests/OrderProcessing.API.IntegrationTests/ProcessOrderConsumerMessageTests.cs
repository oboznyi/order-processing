using FluentAssertions;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.API.Application.Commands;
using OrderProcessing.API.Application.Integrations.EventHandlers;
using OrderProcessing.API.Application.Services;
using OrderProcessing.API.Domain.Inventory;
using OrderProcessing.API.Domain.Orders;
using OrderProcessing.API.Infrastructure.Metrics;
using OrderProcessing.API.Infrastructure.Persistence;
using OrderProcessing.API.Infrastructure.Persistence.Repositories;
using OrderProcessing.API.IntegrationTests.Infrastructure.Fixtures;
using Xunit;

namespace OrderProcessing.API.IntegrationTests;

public sealed class ProcessOrderConsumerMessageTests :
    IClassFixture<PostgresFixture>,
    IClassFixture<MassTransitFixture>
{
    private readonly PostgresFixture _postgresFixture;
    private readonly MassTransitFixture _massTransitFixture;

    public ProcessOrderConsumerMessageTests(
        PostgresFixture postgresFixture,
        MassTransitFixture massTransitFixture)
    {
        _postgresFixture = postgresFixture;
        _massTransitFixture = massTransitFixture;
    }

    [Fact]
    public async Task Consume_ShouldProcessOrderAndDecreaseInventory()
    {
        await using var serviceProvider = BuildServiceProvider();
        await _postgresFixture.InitializeDatabaseAsync(serviceProvider);
        await _postgresFixture.PurgeAsync(serviceProvider);

        var harness = _massTransitFixture.GetHarness();
        harness.Consumer<ProcessOrderConsumer>(serviceProvider);

        await harness.Start();
        try
        {
            var productId = "1";
            var order = await CreateOrderAsync(serviceProvider, "customer-100", productId, 2, 20m);

            await harness.Send(new OrderCreatedEvent(order.Id), Guid.NewGuid());
            (await harness.Consumed.Any<OrderCreatedEvent>()).Should().BeTrue();

            var processedOrder = await LoadOrderAsync(serviceProvider, order.Id);
            processedOrder.Status.Should().Be(OrderStatus.Processed);

            var inventory = await LoadInventoryAsync(serviceProvider, productId);
            inventory.QuantityAvailable.Should().Be(98);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_ShouldMarkOrderAsFailed_WhenInventoryIsNotEnough()
    {
        await using var serviceProvider = BuildServiceProvider();
        await _postgresFixture.InitializeDatabaseAsync(serviceProvider);
        await _postgresFixture.PurgeAsync(serviceProvider);

        var harness = _massTransitFixture.GetHarness();
        harness.Consumer<ProcessOrderConsumer>(serviceProvider);

        await harness.Start();
        try
        {
            var productId = "2";
            var order = await CreateOrderAsync(serviceProvider, "customer-100", productId, 999, 9990m);

            await harness.Send(new OrderCreatedEvent(order.Id), Guid.NewGuid());

            var failedOrder = await LoadOrderAsync(serviceProvider, order.Id);
            failedOrder.Status.Should().Be(OrderStatus.Failed);

            var inventory = await LoadInventoryAsync(serviceProvider, productId);
            inventory.QuantityAvailable.Should().Be(50);
        }
        finally
        {
            await harness.Stop();
        }
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
        {
            options
                .UseNpgsql(_postgresFixture.ConnectionString)
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddSingleton<IOrderMetrics, OrderMetrics>();
        services.AddLogging();

        return services.BuildServiceProvider();
    }

    private static async Task<Order> CreateOrderAsync(
        ServiceProvider serviceProvider,
        string customerId,
        string productId,
        int quantity,
        decimal totalAmount)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = new Order(
            customerId,
            [new OrderItem(productId, quantity, 10m)],
            totalAmount);

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        return order;
    }

    private static async Task<Order> LoadOrderAsync(ServiceProvider serviceProvider, Guid orderId)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Orders
            .AsNoTracking()
            .SingleAsync(x => x.Id == orderId);
    }

    private static async Task<InventoryItem> LoadInventoryAsync(ServiceProvider serviceProvider, string productId)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.InventoryItems
            .AsNoTracking()
            .SingleAsync(x => x.ProductId == productId);
    }
}