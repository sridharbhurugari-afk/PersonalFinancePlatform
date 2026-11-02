using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PFP.Contracts.Exceptions;

namespace PFP.BuildingBlocks.Middleware;

/// <summary>
/// Converts every unhandled exception into an RFC 7807 <see cref="ProblemDetails"/> response.
/// Stack traces and internal messages are only logged, never returned to the client.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    private const string ProblemTypeBase = "https://pfp.example.com/problems/";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected: nothing to respond to, not an error.
            logger.LogDebug(ex, "Request {Method} {Path} was cancelled by the client.", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 499;
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        var problem = Map(ex);
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = traceId;
        if (context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var correlationId))
        {
            problem.Extensions["correlationId"] = correlationId;
        }

        var status = problem.Status ?? StatusCodes.Status500InternalServerError;
        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning("Request {Method} {Path} failed with {Status} ({ErrorCode}): {Message}",
                context.Request.Method, context.Request.Path, status, problem.Extensions["errorCode"], ex.Message);
        }

        // Developers get the exception type name only; never the message or stack trace.
        if (environment.IsDevelopment() && status >= StatusCodes.Status500InternalServerError)
        {
            problem.Extensions["exceptionType"] = ex.GetType().Name;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }

    private static ProblemDetails Map(Exception ex) => ex switch
    {
        ValidationException vex => ValidationProblem(vex),
        AppException app => Create(app.StatusCode, app.ErrorCode, app.Message),
        UnauthorizedAccessException => Create(StatusCodes.Status401Unauthorized, "unauthorized", "Authentication is required."),
        BadHttpRequestException => Create(StatusCodes.Status400BadRequest, "bad_request", "The request is malformed."),
        _ => Create(StatusCodes.Status500InternalServerError, "internal_error",
            "An unexpected error occurred. Please contact support and quote the traceId."),
    };

    private static ProblemDetails ValidationProblem(ValidationException ex)
    {
        var problem = Create(StatusCodes.Status400BadRequest, "validation_failed", "One or more validation errors occurred.");
        problem.Extensions["errors"] = ex.Errors
            .GroupBy(e => e.PropertyName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        return problem;
    }

    private static ProblemDetails Create(int status, string errorCode, string detail) => new()
    {
        Status = status,
        Type = ProblemTypeBase + errorCode.Replace('_', '-'),
        Title = ReasonPhrases(status),
        Detail = detail,
        Extensions = { ["errorCode"] = errorCode },
    };

    private static string ReasonPhrases(int status) => status switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        _ => status >= 500 ? "Internal Server Error" : "Request Failed",
    };
}
