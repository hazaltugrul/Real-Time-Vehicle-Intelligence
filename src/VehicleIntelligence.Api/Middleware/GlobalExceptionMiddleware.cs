using System.Net;
using System.Text.Json;

namespace VehicleIntelligence.Api.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Catches all unhandled exceptions and returns a structured error response.
/// Prevents stack traces from leaking to clients in production.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var response = new
        {
            statusCode = context.Response.StatusCode,
            message = exception switch
            {
                ArgumentException argEx => argEx.Message,
                UnauthorizedAccessException => "Unauthorized.",
                KeyNotFoundException => "Resource not found.",
                _ => "An unexpected error occurred. Please try again later."
            },
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
