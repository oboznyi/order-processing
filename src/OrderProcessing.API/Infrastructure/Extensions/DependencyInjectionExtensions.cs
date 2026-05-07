using MediatR;
using MassTransit;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderProcessing.API.Application.Commands;
using OrderProcessing.API.Application.Behaviors;
using OrderProcessing.API.Application.Integrations.EventHandlers;
using OrderProcessing.API.Application.Services;
using OrderProcessing.API.Infrastructure.Metrics;
using OrderProcessing.API.Infrastructure.RabbitMq;
using OrderProcessing.API.Infrastructure.Persistence;
using OrderProcessing.API.Infrastructure.Persistence.Repositories;
using OrderProcessing.API.Mapping;

namespace OrderProcessing.API.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(Program).Assembly);
        });
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options
                .UseNpgsql(configuration.GetConnectionString("Postgres"))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderReadRepository, OrderReadRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IDiscountService, DiscountService>();

        services.AddSingleton<IOrderMetrics, OrderMetrics>();
        services.AddHostedService<OutboxPublisherBackgroundService>();
        services.AddHostedService<OutboxCleanupBackgroundService>();

        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.Configure<OutboxCleanupOptions>(configuration.GetSection("OutboxCleanup"));

        services.AddMassTransit(x =>
        {
            x.AddConsumer<ProcessOrderConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqOptions = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                var virtualHost = rabbitMqOptions.VirtualHost.TrimStart('/');
                var hostUri = string.IsNullOrWhiteSpace(virtualHost)
                    ? $"rabbitmq://{rabbitMqOptions.Host}:{rabbitMqOptions.Port}"
                    : $"rabbitmq://{rabbitMqOptions.Host}:{rabbitMqOptions.Port}/{virtualHost}";

                cfg.Host(new Uri(hostUri), h =>
                {
                    h.Username(rabbitMqOptions.Username);
                    h.Password(rabbitMqOptions.Password);
                });

                cfg.ReceiveEndpoint(rabbitMqOptions.QueueName, e =>
                {
                    e.PrefetchCount = 16;
                    e.ConcurrentMessageLimit = 8;

                    e.UseMessageRetry(r =>
                    {
                        r.Interval(3, TimeSpan.FromSeconds(5));
                    });

                    e.ConfigureConsumer<ProcessOrderConsumer>(context);
                });
            });
        });

        return services;
    }
}
