using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Api.Middleware;

/// <summary>Converts unhandled exceptions into RFC 7807 problem responses.</summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;

    /// <summary>Initializes the exception handling middleware.</summary>
    /// <param name="next">Next middleware in the pipeline.</param>
    /// <param name="logger">Logger used for exception diagnostics.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    /// <summary>Processes the request and handles unhandled exceptions.</summary>
    /// <param name="context">HTTP context being processed.</param>
    /// <returns>A task representing request processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var statusCode = MapStatusCode(exception);
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}. TraceId: {TraceId}.",
                context.Request.Method,
                context.Request.Path,
                traceId);

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitle(statusCode),
                Detail = statusCode == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred while processing the request."
                    : exception.Message,
                Instance = context.Request.Path
            };

            problemDetails.Extensions["traceId"] = traceId;

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json");
        }
    }

    private static int MapStatusCode(Exception exception) =>
        exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            BadHttpRequestException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

    private static string GetTitle(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not Found",
            _ => "Internal Server Error"
        };
}
