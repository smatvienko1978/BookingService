using System.Diagnostics;

namespace BookingService.Api.Middleware;

/// <summary>
/// Adds TraceId and RequestId to the logging scope for the duration of the request
/// so all log entries include correlation identifiers.
/// </summary>
public sealed class RequestLoggingScopeMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingScopeMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.TraceIdentifier;
        var requestId = Activity.Current?.Id ?? context.Request.Headers["X-Request-ID"].FirstOrDefault() ?? traceId;

        using (context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("Request").BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = traceId,
            ["RequestId"] = requestId
        }))
        {
            await _next(context);
        }
    }
}
