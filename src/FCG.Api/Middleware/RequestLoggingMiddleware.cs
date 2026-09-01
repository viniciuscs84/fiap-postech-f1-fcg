using System.Diagnostics;

namespace FCG.Api.Middleware;

/// <summary>Logs HTTP request timing and adds a correlation identifier to responses.</summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<RequestLoggingMiddleware> logger;

    /// <summary>Initializes the request logging middleware.</summary>
    /// <param name="next">Next middleware in the pipeline.</param>
    /// <param name="logger">Logger used for request diagnostics.</param>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    /// <summary>Processes the current HTTP request.</summary>
    /// <param name="context">HTTP context being processed.</param>
    /// <returns>A task representing request processing.</returns>
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
