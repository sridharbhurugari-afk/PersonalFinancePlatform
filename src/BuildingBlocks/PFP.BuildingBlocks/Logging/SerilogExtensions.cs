using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

namespace PFP.BuildingBlocks.Logging;

public static class SerilogExtensions
{
    /// <summary>
    /// Bootstrap logger used until the host is built, so start-up failures are captured as Fatal.
    /// </summary>
    public static void CreateBootstrapLogger(string serviceName) =>
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(new RenderedCompactJsonFormatter())
            .CreateBootstrapLogger();

    /// <summary>
    /// Configures Serilog from the "Serilog" configuration section (levels/sinks can be changed per
    /// environment via appsettings.{Environment}.json or env vars such as Serilog__MinimumLevel__Default)
    /// and adds the enrichers common to every service.
    /// </summary>
    public static WebApplicationBuilder AddPlatformSerilog(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Host.UseSerilog((context, services, logger) => logger
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Service", serviceName)
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
            .WriteTo.Console(new RenderedCompactJsonFormatter()));

        return builder;
    }

    /// <summary>Logs one structured event per HTTP request, enriched with user and client info.</summary>
    public static WebApplication UsePlatformRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnostic, http) =>
            {
                diagnostic.Set("UserId", http.User.FindFirst("sub")?.Value ?? "anonymous");
                diagnostic.Set("ClientIp", http.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            };
        });
        return app;
    }
}
