
using System.Text.Json;

namespace CoffeeNChill.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception. TraceId: {TraceId}",
                context.TraceIdentifier);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode =
                StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new
                {
                    error = "An unexpected server error occurred.",
                    traceId = context.TraceIdentifier
                }));
        }
    }
}
