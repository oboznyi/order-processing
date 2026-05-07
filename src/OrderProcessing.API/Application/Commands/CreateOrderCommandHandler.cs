using System.Text.Json;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.API.Application.Extensions;
using OrderProcessing.API.Domain.Messaging;
using OrderProcessing.API.Domain.Orders;
using OrderProcessing.API.Infrastructure.Persistence;

namespace OrderProcessing.API.Application.Commands;

public sealed class CreateOrderCommandHandler
    : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IOutboxRepository outboxRepository,
        IIdempotencyRepository idempotencyRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _outboxRepository = outboxRepository;
        _idempotencyRepository = idempotencyRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CreateOrderResult> Handle(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            var existingOrderId = await _idempotencyRepository.GetOrderIdAsync(
                command.IdempotencyKey,
                cancellationToken);

            if (existingOrderId is not null)
                return new CreateOrderResult(existingOrderId.Value, OrderStatus.Pending);
        }

        var order = _mapper.Map<Order>(command);
        var @event = new OrderCreatedEvent(order.Id);

        var outboxMessage = new OutboxMessage(
            nameof(OrderCreatedEvent),
            JsonSerializer.Serialize(@event),
            command.CorrelationId);

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                _orderRepository.Add(order);
                _outboxRepository.Add(outboxMessage);

                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                    _idempotencyRepository.Add(new IdempotencyKey(command.IdempotencyKey, order.Id));

                await _unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);
        }
        catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            var existingOrderId = await _idempotencyRepository.GetOrderIdAsync(
                command.IdempotencyKey!,
                cancellationToken);

            if (existingOrderId is not null)
                return new CreateOrderResult(existingOrderId.Value, OrderStatus.Pending);

            throw;
        }

        _logger.LogOrderCreated(order, command.CorrelationId);

        return new CreateOrderResult(order.Id, order.Status);
    }
}