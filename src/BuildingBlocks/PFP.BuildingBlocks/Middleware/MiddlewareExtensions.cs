using Microsoft.AspNetCore.Builder;

namespace PFP.BuildingBlocks.Middleware;

public static class MiddlewareExtensions
{
    /// <summary>Registers correlation-id then global exception handling. Call before routing/auth.</summary>
    public static IApplicationBuilder UsePlatformMiddleware(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>()
           .UseMiddleware<GlobalExceptionHandlingMiddleware>();
}
