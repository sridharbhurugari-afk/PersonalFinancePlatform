using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PFP.BuildingBlocks.Logging;
using PFP.BuildingBlocks.Middleware;
using PFP.Expense.Api.Endpoints;
using PFP.Expense.Api.Persistence;
using PFP.Expense.Infrastructure.Persistence;
using Serilog;

const string ServiceName = "expense-service";

SerilogExtensions.CreateBootstrapLogger(ServiceName);

try
{
    Log.Information("Starting {Service}", ServiceName);

    var builder = WebApplication.CreateBuilder(args);
    builder.AddPlatformSerilog(ServiceName);

    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddOpenApi();
    builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    builder.Services.AddDbContext<ExpenseDbContext>(options => options
        .UseSqlServer(
            builder.Configuration.GetConnectionString("ExpenseDb"),
            sql => sql.EnableRetryOnFailure(maxRetryCount: 5)));
    builder.Services.AddHealthChecks().AddDbContextCheck<ExpenseDbContext>("expense-db");
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

    var app = builder.Build();

    app.UsePlatformMiddleware();
    app.UsePlatformRequestLogging();
    app.UseCors();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();

        // Dev convenience only: applies pending migrations and seeds demo data so the dashboard has something to show.
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        await db.Database.MigrateAsync();
        await ExpenseDbSeeder.SeedAsync(db, TimeProvider.System);
    }

    app.MapHealthChecks("/health");
    app.MapExpenseEndpoints();

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
