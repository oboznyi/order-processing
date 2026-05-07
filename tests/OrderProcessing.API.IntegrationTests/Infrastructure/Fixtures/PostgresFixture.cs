using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.API.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace OrderProcessing.API.IntegrationTests.Infrastructure.Fixtures
{
    public sealed class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container;

        public PostgresFixture()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.Testing.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var image = GetSetting(configuration, "Image", "postgres:16");
            var database = GetSetting(configuration, "Database", "orders_test_db");
            var username = GetSetting(configuration, "Username", "postgres");
            var password = GetSetting(configuration, "Password", "postgres");

            _container = new PostgreSqlBuilder()
                .WithImage(image)
                .WithDatabase(database)
                .WithUsername(username)
                .WithPassword(password)
                .Build();
        }

        public string ConnectionString => _container.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
        }

        public async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.MigrateAsync();
        }

        public async Task PurgeAsync(IServiceProvider serviceProvider)
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                processed_messages,
                outbox_messages,
                idempotency_keys,
                order_items,
                orders
            RESTART IDENTITY CASCADE;
        """);

            await dbContext.Database.ExecuteSqlRawAsync("""
            UPDATE inventory_items
            SET quantity_available = CASE
                WHEN product_id = '1' THEN 100
                WHEN product_id = '2' THEN 50
                ELSE quantity_available
            END;
        """);
        }

        private static string GetSetting(IConfiguration configuration, string key, string fallback)
        {
            return configuration[$"Testcontainers:Postgres:{key}"]
                   ?? configuration[$"TESTCONTAINERS_POSTGRES_{key.ToUpperInvariant()}"]
                   ?? fallback;
        }
    }
}
