using AutoMapper;
using OrderProcessing.API.ApiModels.Requests;
using OrderProcessing.API.Application.DTOs;
using OrderProcessing.API.Application.Commands;
using OrderProcessing.API.Domain.Orders;

namespace OrderProcessing.API.Mapping;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateOrderItemRequest, CreateOrderItemCommand>();

        CreateMap<CreateOrderRequest, CreateOrderCommand>()
            .ConstructUsing(src => new CreateOrderCommand(
                src.CustomerId,
                src.Items.Select(i => new CreateOrderItemCommand(
                    i.ProductId,
                    i.Quantity,
                    i.UnitPrice)).ToList(),
                src.TotalAmount,
                null,
                null));

        CreateMap<CreateOrderItemCommand, OrderItem>();

        CreateMap<CreateOrderCommand, Order>()
            .ConstructUsing(src => new Order(
                src.CustomerId,
                src.Items.Select(i => new OrderItem(
                    i.ProductId,
                    i.Quantity,
                    i.UnitPrice)).ToList(),
                src.TotalAmount));

        CreateMap<OrderItem, OrderItemResponse>();
        CreateMap<Order, OrderResponse>();
    }
}