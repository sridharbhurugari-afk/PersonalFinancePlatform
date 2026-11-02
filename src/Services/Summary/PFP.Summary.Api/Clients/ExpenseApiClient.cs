using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PFP.Summary.Api.Contracts;

namespace PFP.Summary.Api.Clients;

/// <summary>Typed client for the Expense service's read endpoints. Base address is configured via "ExpenseApi:BaseUrl".</summary>
public sealed class ExpenseApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<IReadOnlyCollection<ExpenseResponse>> GetRecentAsync(Guid userId, int take, CancellationToken ct) =>
        await http.GetFromJsonAsync<IReadOnlyCollection<ExpenseResponse>>(
            $"/api/expenses?userId={userId}&take={take}", JsonOptions, ct) ?? [];

    public async Task<ExpensesPeriodSummaryResponse> GetPeriodSummaryAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct) =>
        await http.GetFromJsonAsync<ExpensesPeriodSummaryResponse>(
            $"/api/expenses/summary?userId={userId}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", JsonOptions, ct)
        ?? new ExpensesPeriodSummaryResponse(from, to, 0, []);
}
