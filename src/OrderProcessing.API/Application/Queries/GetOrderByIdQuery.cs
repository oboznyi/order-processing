using MediatR;
using OrderProcessing.API.Application.DTOs;

namespace OrderProcessing.API.Application.Queries;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderResponse?>;