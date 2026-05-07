using Microsoft.EntityFrameworkCore;
using OrderProcessing.API.Domain.Orders;
using OrderProcessing.API.Infrastructure.Persistence;

namespace OrderProcessing.API.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _dbContext;

    public OrderRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Order order)
    {
        _dbContext.Orders.Add(order);
    }

    public Task<Order?> GetByIdWithItemsAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return _dbContext.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
    }
}
