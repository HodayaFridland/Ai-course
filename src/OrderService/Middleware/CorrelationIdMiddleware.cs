namespace OrderService.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        const string headerName = "X-Correlation-Id";
        var correlationId = context.Request.Headers[headerName].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[headerName] = correlationId;

        using (_logger.BeginScope("CorrelationId:{CorrelationId}", correlationId))
        {
            _logger.LogInformation("Handling request {Method} {Path}", context.Request.Method, context.Request.Path);
            await _next(context);
        }
    }
}
