namespace VehicleIntelligence.Api.Middleware;

/// <summary>
/// Middleware that ensures every request has a unique CorrelationId.
/// Reads from X-Correlation-Id header if present, otherwise generates a new GUID.
/// Sets the header on the response for traceability.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Items[CorrelationIdHeader] = correlationId;
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);

        // Make available for Serilog enrichment
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
