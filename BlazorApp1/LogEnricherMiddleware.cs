using Serilog.Context;

public class LogEnricherMiddleware
{
    private readonly RequestDelegate _next;

    public LogEnricherMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.TraceIdentifier; // Or generate a new Guid
        var sessionId = context.Session.Id; // If using session, or use circuit ID for Blazor Server

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("SessionId", sessionId))
        {
            await _next(context);
        }
    }
}
