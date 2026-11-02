using PFP.BuildingBlocks.Logging;
using PFP.BuildingBlocks.Middleware;
using Serilog;

const string ServiceName = "auth-service";

SerilogExtensions.CreateBootstrapLogger(ServiceName);

try
{
    Log.Information("Starting {Service}", ServiceName);

    var builder = WebApplication.CreateBuilder(args);
    builder.AddPlatformSerilog(ServiceName);

    builder.Services.AddOpenApi();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UsePlatformMiddleware();
    app.UsePlatformRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapHealthChecks("/health");

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "{Service} terminated unexpectedly", ServiceName);
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;

public partial class Program;
