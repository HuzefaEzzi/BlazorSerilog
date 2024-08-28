using BlazorApp1.Components;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Diagnostics;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "YourAppName")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Use session middleware
app.UseSession();

// Add middleware to generate Correlation ID and Session ID
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.ContainsKey("X-Correlation-ID"))
    {
        context.Request.Headers["X-Correlation-ID"] = Activity.Current?.Id ?? context.TraceIdentifier;
    }
    
    if (!context.Request.Headers.ContainsKey("X-Session-ID"))
    {
        context.Request.Headers["X-Session-ID"] = context.Session.Id ?? Guid.NewGuid().ToString();
    }
    
    await next();
});

// Configure global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;
        
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        var sessionId = context.Request.Headers["X-Session-ID"].FirstOrDefault();
        
        Log.Error(exception, "An unhandled exception occurred. Correlation ID: {CorrelationId}, Session ID: {SessionId}", correlationId, sessionId);
        
        context.Response.Redirect($"/Error?correlationId={correlationId}&sessionId={sessionId}");
    });
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseMiddleware<LogEnricherMiddleware>();

app.Run();

// Ensure to flush and close the log when the application shuts down
Log.CloseAndFlush();
