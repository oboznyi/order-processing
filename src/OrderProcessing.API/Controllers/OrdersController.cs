using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderProcessing.API.ApiModels.Requests;
using OrderProcessing.API.ApiModels.Responses;
using OrderProcessing.API.Application.DTOs;
using OrderProcessing.API.Application.Commands;
using OrderProcessing.API.Application.Queries;
using OrderProcessing.API.Common.Constants;

namespace OrderProcessing.API.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public OrdersController(
        IMediator mediator,
        IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder(
        CreateOrderRequest request,
        [FromHeader(Name = HeaderNames.IdempotencyKey)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Items[HeaderNames.CorrelationId]?.ToString();

        var mappedCommand = _mapper.Map<CreateOrderCommand>(request);

        var command = mappedCommand with
        {
            IdempotencyKey = idempotencyKey,
            CorrelationId = correlationId
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Accepted(new CreateOrderResponse(result.OrderId, result.Status));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _mediator.Send(
            new GetOrderByIdQuery(id),
            cancellationToken);

        return order is null ? NotFound() : Ok(order);
    }
}