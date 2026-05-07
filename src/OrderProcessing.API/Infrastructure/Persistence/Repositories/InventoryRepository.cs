using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderProcessing.API.Domain.Orders;
using OrderProcessing.API.Infrastructure.Persistence;

namespace OrderProcessing.API.Infrastructure.Persistence.Repositories;

public sealed class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _dbContext;

    public InventoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryReserveInventoryAsync(
        IReadOnlyCollection<OrderItem> items,
        CancellationToken cancellationToken)
    {
        var requestedItems = items
            .GroupBy(x => x.ProductId)
            .Select(x => new
            {
                ProductId = x.Key,
                Quantity = x.Sum(i => i.Quantity)
            })
            .ToList();

        if (requestedItems.Count == 0)
            return false;

        var valueRows = new List<string>(requestedItems.Count);
        var parameters = new List<NpgsqlParameter>(requestedItems.Count * 2);

        for (var i = 0; i < requestedItems.Count; i++)
        {
            var productParameter = new NpgsqlParameter($"@p{i}", requestedItems[i].ProductId);
            var quantityParameter = new NpgsqlParameter($"@q{i}", requestedItems[i].Quantity);

            valueRows.Add($"({productParameter.ParameterName}, {quantityParameter.ParameterName})");
            parameters.Add(productParameter);
            parameters.Add(quantityParameter);
        }

        var sql = $"""
            WITH requested(product_id, qty) AS (
                VALUES {string.Join(", ", valueRows)}
            ),
            requested_count AS (
                SELECT COUNT(*) AS count FROM requested
            ),
            available_count AS (
                SELECT COUNT(*) AS count
                FROM inventory_items i
                JOIN requested r ON r.product_id = i.product_id
                WHERE i.quantity_available >= r.qty
            ),
            can_reserve AS (
                SELECT
                    (SELECT count FROM requested_count) = (SELECT count FROM available_count) AS ok
            )
            UPDATE inventory_items i
            SET quantity_available = i.quantity_available - r.qty
            FROM requested r
            WHERE i.product_id = r.product_id
              AND (SELECT ok FROM can_reserve)
            """;

        var affectedRows = await _dbContext.Database.ExecuteSqlRawAsync(
            sql,
            parameters,
            cancellationToken);

        return affectedRows == requestedItems.Count;
    }
}