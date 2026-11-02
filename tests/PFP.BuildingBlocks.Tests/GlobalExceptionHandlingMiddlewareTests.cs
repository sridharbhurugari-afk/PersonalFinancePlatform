using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PFP.BuildingBlocks.Middleware;
using PFP.Contracts.Exceptions;

namespace PFP.BuildingBlocks.Tests;

public class GlobalExceptionHandlingMiddlewareTests
{
    private static (DefaultHttpContext Context, MemoryStream Body) CreateContext()
    {
        var body = new MemoryStream();
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };
        context.Request.Path = "/api/expenses";
        context.Response.Body = body;
        return (context, body);
    }

    private static GlobalExceptionHandlingMiddleware CreateSut(RequestDelegate next, string environment = "Production")
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(environment);
        return new GlobalExceptionHandlingMiddleware(next, NullLogger<GlobalExceptionHandlingMiddleware>.Instance, env);
    }

    private static JsonElement ReadJson(MemoryStream body)
    {
        body.Position = 0;
        return JsonDocument.Parse(body).RootElement;
    }

    [Fact]
    public async Task Unhandled_exception_returns_generic_500_without_leaking_details()
    {
        var (context, body) = CreateContext();
        var sut = CreateSut(_ => throw new InvalidOperationException("secret connection string xyz"));

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().StartWith("application/problem+json");
        var json = ReadJson(body);
        json.GetProperty("errorCode").GetString().Should().Be("internal_error");
        json.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        json.ToString().Should().NotContain("secret").And.NotContain("InvalidOperationException");
    }

    [Fact]
    public async Task Development_environment_exposes_exception_type_only()
    {
        var (context, body) = CreateContext();
        var sut = CreateSut(_ => throw new InvalidOperationException("boom"), Environments.Development);

        await sut.InvokeAsync(context);

        var json = ReadJson(body);
        json.GetProperty("exceptionType").GetString().Should().Be(nameof(InvalidOperationException));
        json.ToString().Should().NotContain("boom");
    }

    [Fact]
    public async Task NotFoundException_maps_to_404_with_error_code()
    {
        var (context, body) = CreateContext();
        var sut = CreateSut(_ => throw new NotFoundException("Expense", 42));

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(404);
        ReadJson(body).GetProperty("errorCode").GetString().Should().Be("resource_not_found");
    }

    [Fact]
    public async Task ValidationException_maps_to_400_with_grouped_errors()
    {
        var (context, body) = CreateContext();
        var failures = new[]
        {
            new ValidationFailure("Amount", "Amount must be positive."),
            new ValidationFailure("Amount", "Amount must have two decimals."),
            new ValidationFailure("Category", "Category is required."),
        };
        var sut = CreateSut(_ => throw new ValidationException(failures));

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
        var errors = ReadJson(body).GetProperty("errors");
        errors.GetProperty("Amount").GetArrayLength().Should().Be(2);
        errors.GetProperty("Category").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Client_cancellation_is_not_reported_as_an_error()
    {
        var (context, _) = CreateContext();
        using var cts = new CancellationTokenSource();
        context.RequestAborted = cts.Token;
        var sut = CreateSut(_ =>
        {
            cts.Cancel();
            throw new OperationCanceledException(cts.Token);
        });

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(499);
    }

    [Fact]
    public async Task Request_without_exception_passes_through()
    {
        var (context, _) = CreateContext();
        var sut = CreateSut(ctx =>
        {
            ctx.Response.StatusCode = 204;
            return Task.CompletedTask;
        });

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(204);
    }
}
