using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.API.Application.DTOs;
using OrderProcessing.API.Infrastructure.Persistence;

namespace OrderProcessing.API.Infrastructure.Persistence.Repositories;

public sealed class OrderReadRepository : IOrderReadRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;

    public OrderReadRepository(AppDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        return order is null ? null : _mapper.Map<OrderResponse>(order);
    }
}
