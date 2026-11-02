using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PFP.Summary.Api.Contracts;

namespace PFP.Summary.Api.Clients;

/// <summary>Typed client for the Budget service's read endpoints. Base address is configured via "BudgetApi:BaseUrl".</summary>
public sealed class BudgetApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<IReadOnlyCollection<BudgetResponse>> GetActiveAsync(Guid userId, CancellationToken ct) =>
        await http.GetFromJsonAsync<IReadOnlyCollection<BudgetResponse>>(
            $"/api/budgets?userId={userId}&activeOnly=true", JsonOptions, ct) ?? [];
}
