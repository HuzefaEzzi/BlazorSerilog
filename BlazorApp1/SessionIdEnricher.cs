using Serilog.Core;
using Serilog.Events;
using Microsoft.AspNetCore.Http;

public class SessionIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var sessionId = _httpContextAccessor.HttpContext?.Request.Headers["X-Session-ID"].FirstOrDefault();
        if (!string.IsNullOrEmpty(sessionId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SessionId", sessionId));
        }
    }
}
