using Microsoft.AspNetCore.Http;
using OrderProcessing.API.Common.Constants;

namespace OrderProcessing.API.Infrastructure.Middlewares;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderNames.CorrelationId, out var value)
            ? value.ToString()
            : Guid.NewGuid().ToString();

        context.Items[HeaderNames.CorrelationId] = correlationId;
        context.Response.Headers[HeaderNames.CorrelationId] = correlationId;

        await _next(context);
    }
}
