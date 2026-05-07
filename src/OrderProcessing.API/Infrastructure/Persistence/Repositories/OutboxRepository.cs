using Microsoft.EntityFrameworkCore;
using OrderProcessing.API.Domain.Messaging;
using OrderProcessing.API.Infrastructure.Persistence;

namespace OrderProcessing.API.Infrastructure.Persistence.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _dbContext;

    public OutboxRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(OutboxMessage message)
    {
        _dbContext.OutboxMessages.Add(message);
    }

    public Task<List<OutboxMessage>> GetUnpublishedForUpdateAsync(
        int take,
        CancellationToken cancellationToken)
    {
        return _dbContext.OutboxMessages
            .FromSqlRaw(
                """
                SELECT *
                FROM outbox_messages
                WHERE published_at_utc IS NULL
                  AND retry_count < 5
                ORDER BY created_at_utc
                FOR UPDATE SKIP LOCKED
                LIMIT {0}
                """,
                take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> DeletePublishedOlderThanAsync(
        DateTime thresholdUtc,
        int take,
        CancellationToken cancellationToken)
    {
        return _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             DELETE FROM outbox_messages
             WHERE id IN (
                 SELECT id
                 FROM outbox_messages
                 WHERE published_at_utc IS NOT NULL
                   AND published_at_utc < {thresholdUtc}
                 ORDER BY published_at_utc
                 LIMIT {take}
             )
             """,
            cancellationToken);
    }
}
