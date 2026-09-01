using System.Diagnostics;

namespace FCG.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<RequestLoggingMiddleware> logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] = traceId;
            return Task.CompletedTask;
        });

        await next(context);

        logger.LogInformation(
            "HTTP {Method} {Path} responded with {StatusCode} in {ElapsedMs} ms. TraceId: {TraceId}.",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            traceId);
    }
}
