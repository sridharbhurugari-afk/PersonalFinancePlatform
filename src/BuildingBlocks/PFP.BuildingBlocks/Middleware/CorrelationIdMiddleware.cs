using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace PFP.BuildingBlocks.Middleware;

/// <summary>
/// Reads (or generates) a correlation id, echoes it on the response and pushes it into the Serilog
/// <see cref="LogContext"/> so every log line of the request carries it. Must be the first middleware.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Resolve(context.Request.Headers[HeaderName].ToString());
        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    // Incoming values are untrusted: accept only short, log-safe tokens to prevent log forging.
    private static string Resolve(string incoming) =>
        incoming.Length is > 0 and <= MaxLength && incoming.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? incoming
            : Guid.NewGuid().ToString("N");
}
