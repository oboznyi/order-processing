using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.API.Application.Commands;
using OrderProcessing.API.Domain.Orders;
using OrderProcessing.API.Infrastructure.Persistence;
using OrderProcessing.API.Infrastructure.Persistence.Repositories;
using OrderProcessing.API.IntegrationTests.Infrastructure.Fixtures;
using OrderProcessing.API.Mapping;
using Xunit;

namespace OrderProcessing.API.IntegrationTests;

public sealed class CreateOrderCommandTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgresFixture;

    public CreateOrderCommandTests(PostgresFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
    }

    [Fact]
    public async Task Should_Create_Order_And_Outbox()
    {
        await using var serviceProvider = BuildServiceProvider();
        await _postgresFixture.InitializeDatabaseAsync(serviceProvider);
        await _postgresFixture.PurgeAsync(serviceProvider);

        await using var scope = serviceProvider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cmd = new CreateOrderCommand(
            "customer",
            [new CreateOrderItemCommand("1", 2, 10m)],
            20m,
            "key-1",
            "corr-1");

        await mediator.Send(cmd);

        var order = await db.Orders.Include(x => x.Items).SingleAsync();
        order.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().HaveCount(1);

        var outbox = await db.OutboxMessages.SingleAsync();
        outbox.Type.Should().Be("OrderCreatedEvent");

        var idem = await db.IdempotencyKeys.SingleAsync();
        idem.Key.Should().Be("key-1");
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

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<CreateOrderCommandHandler>();
        });

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        services.AddLogging();

        return services.BuildServiceProvider();
    }
}