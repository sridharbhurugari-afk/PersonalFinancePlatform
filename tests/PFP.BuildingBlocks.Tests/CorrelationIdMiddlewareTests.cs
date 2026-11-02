using FluentAssertions;
using Microsoft.AspNetCore.Http;
using PFP.BuildingBlocks.Middleware;

namespace PFP.BuildingBlocks.Tests;

public class CorrelationIdMiddlewareTests
{
    private static async Task<DefaultHttpContext> RunAsync(string? incoming)
    {
        var context = new DefaultHttpContext();
        if (incoming is not null)
        {
            context.Request.Headers[CorrelationIdMiddleware.HeaderName] = incoming;
        }

        var sut = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await sut.InvokeAsync(context);
        return context;
    }

    [Fact]
    public async Task Valid_incoming_id_is_preserved()
    {
        var context = await RunAsync("abc-123_XYZ");

        context.Items[CorrelationIdMiddleware.ItemKey].Should().Be("abc-123_XYZ");
    }

    [Fact]
    public async Task Missing_id_is_generated()
    {
        var context = await RunAsync(null);

        context.Items[CorrelationIdMiddleware.ItemKey].Should().BeOfType<string>().Which.Should().HaveLength(32);
    }

    [Theory]
    [InlineData("bad value\r\nforged=1")]
    [InlineData("<script>alert(1)</script>")]
    public async Task Unsafe_incoming_id_is_replaced(string incoming)
    {
        var context = await RunAsync(incoming);

        context.Items[CorrelationIdMiddleware.ItemKey].Should().NotBe(incoming);
    }
}
