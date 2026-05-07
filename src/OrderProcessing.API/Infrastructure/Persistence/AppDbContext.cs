using Microsoft.EntityFrameworkCore;
using OrderProcessing.API.Domain.Inventory;
using OrderProcessing.API.Domain.Messaging;
using OrderProcessing.API.Domain.Orders;

namespace OrderProcessing.API.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("orders");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CustomerId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(x => x.FailureReason)
                .HasMaxLength(500);

            builder.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.ToTable("order_items");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<InventoryItem>(builder =>
        {
            builder.ToTable("inventory_items");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductId)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.ProductId)
                .IsUnique();

            builder.HasData(
                new
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    ProductId = "1",
                    QuantityAvailable = 100
                },
                new
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    ProductId = "2",
                    QuantityAvailable = 50
                });
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type).IsRequired().HasMaxLength(300);
            builder.Property(x => x.Payload).IsRequired();
            builder.Property(x => x.CorrelationId).HasMaxLength(100);
            builder.Property(x => x.Error).HasMaxLength(1000);

            builder.HasIndex(x => new { x.PublishedAtUtc, x.CreatedAtUtc });
        });

        modelBuilder.Entity<ProcessedMessage>(builder =>
        {
            builder.ToTable("processed_messages");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ConsumerName)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(x => new { x.MessageId, x.ConsumerName })
                .IsUnique();
        });

        modelBuilder.Entity<IdempotencyKey>(builder =>
        {
            builder.ToTable("idempotency_keys");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(x => x.Key)
                .IsUnique();
        });
    }
}
