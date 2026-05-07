using Microsoft.Extensions.Logging;
using OrderProcessing.API.Domain.Orders;

namespace OrderProcessing.API.Application.Extensions;

public static class LoggerExtensions
{
    public static void LogOrderCreated(
        this ILogger logger,
        Order order,
        string? correlationId)
    {
        logger.LogInformation(
            "Order submitted. OrderId: {OrderId}, CustomerId: {CustomerId}, Status: {Status}, TotalAmount: {TotalAmount}, CorrelationId: {CorrelationId}",
            order.Id,
            order.CustomerId,
            order.Status,
            order.TotalAmount,
            correlationId);
    }

    public static void LogOrderProcessingStarted(
        this ILogger logger,
        Order order,
        string? correlationId)
    {
        logger.LogInformation(
            "Order processing started. OrderId: {OrderId}, CustomerId: {CustomerId}, Status: {Status}, CorrelationId: {CorrelationId}",
            order.Id,
            order.CustomerId,
            order.Status,
            correlationId);
    }

    public static void LogOrderProcessed(
        this ILogger logger,
        Order order,
        string? correlationId)
    {
        logger.LogInformation(
            "Order processed. OrderId: {OrderId}, CustomerId: {CustomerId}, Status: {Status}, TotalAmount: {TotalAmount}, CorrelationId: {CorrelationId}",
            order.Id,
            order.CustomerId,
            order.Status,
            order.TotalAmount,
            correlationId);
    }

    public static void LogOrderFailed(
        this ILogger logger,
        Order order,
        string reason,
        string? correlationId)
    {
        logger.LogWarning(
            "Order failed. OrderId: {OrderId}, CustomerId: {CustomerId}, Status: {Status}, Reason: {Reason}, CorrelationId: {CorrelationId}",
            order.Id,
            order.CustomerId,
            order.Status,
            reason,
            correlationId);
    }
}
