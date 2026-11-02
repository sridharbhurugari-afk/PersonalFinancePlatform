using System.Text.Json.Serialization;
using PFP.BuildingBlocks.Logging;
using PFP.BuildingBlocks.Middleware;
using PFP.Summary.Api.Clients;
using PFP.Summary.Api.Endpoints;
using Serilog;

const string ServiceName = "summary-service";

SerilogExtensions.CreateBootstrapLogger(ServiceName);

try
{
    Log.Information("Starting {Service}", ServiceName);

    var builder = WebApplication.CreateBuilder(args);
    builder.AddPlatformSerilog(ServiceName);

    builder.Services.AddOpenApi();
    builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddHealthChecks();
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

    builder.Services.AddHttpClient<ExpenseApiClient>(client =>
        client.BaseAddress = new Uri(builder.Configuration["ExpenseApi:BaseUrl"] ?? "http://localhost:5002"));
    builder.Services.AddHttpClient<BudgetApiClient>(client =>
        client.BaseAddress = new Uri(builder.Configuration["BudgetApi:BaseUrl"] ?? "http://localhost:5003"));

    var app = builder.Build();

    app.UsePlatformMiddleware();
    app.UsePlatformRequestLogging();
    app.UseCors();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapHealthChecks("/health");
    app.MapSummaryEndpoints();

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
