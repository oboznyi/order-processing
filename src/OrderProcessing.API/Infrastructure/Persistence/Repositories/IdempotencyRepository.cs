using Microsoft.EntityFrameworkCore;
using OrderProcessing.API.Domain.Messaging;
using OrderProcessing.API.Infrastructure.Persistence;

namespace OrderProcessing.API.Infrastructure.Persistence.Repositories;

public sealed class IdempotencyRepository : IIdempotencyRepository
{
    private readonly AppDbContext _dbContext;

    public IdempotencyRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Guid?> GetOrderIdAsync(string key, CancellationToken cancellationToken)
    {
        return _dbContext.IdempotencyKeys
            .AsNoTracking()
            .Where(x => x.Key == key)
            .Select(x => (Guid?)x.OrderId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(IdempotencyKey key)
    {
        _dbContext.IdempotencyKeys.Add(key);
    }
}