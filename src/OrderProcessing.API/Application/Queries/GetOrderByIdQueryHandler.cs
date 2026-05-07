using MediatR;
using OrderProcessing.API.Application.DTOs;
using OrderProcessing.API.Infrastructure.Persistence;

namespace OrderProcessing.API.Application.Queries;

public sealed class GetOrderByIdQueryHandler: IRequestHandler<GetOrderByIdQuery, OrderResponse?>
{
    private readonly IOrderReadRepository _orderReadRepository;

    public GetOrderByIdQueryHandler(IOrderReadRepository orderReadRepository)
    {
        _orderReadRepository = orderReadRepository;
    }

    public Task<OrderResponse?> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _orderReadRepository.GetByIdAsync(
            request.OrderId,
            cancellationToken);
    }
}