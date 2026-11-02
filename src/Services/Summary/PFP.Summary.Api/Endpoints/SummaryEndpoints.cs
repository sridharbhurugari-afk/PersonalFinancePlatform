using PFP.Contracts.Common;
using PFP.Summary.Api.Clients;
using PFP.Summary.Api.Contracts;

namespace PFP.Summary.Api.Endpoints;

public static class SummaryEndpoints
{
    public static RouteGroupBuilder MapSummaryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/summary").WithTags("Summary");
        group.MapGet("/", GetSummaryAsync).WithName("GetDashboardSummary");
        return group;
    }

    private static async Task<DashboardSummaryResponse> GetSummaryAsync(
        ExpenseApiClient expenseClient,
        BudgetApiClient budgetClient,
        IConfiguration configuration,
        Guid? userId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct)
    {
        var resolvedUserId = userId ?? DemoUser.Id;
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var periodStart = from ?? new DateOnly(today.Year, today.Month, 1);
        var periodEnd = to ?? periodStart.AddMonths(1).AddDays(-1);

        var expenseSummaryTask = expenseClient.GetPeriodSummaryAsync(resolvedUserId, periodStart, periodEnd, ct);
        var recentTask = expenseClient.GetRecentAsync(resolvedUserId, take: 10, ct);
        var budgetsTask = budgetClient.GetActiveAsync(resolvedUserId, ct);
        await Task.WhenAll(expenseSummaryTask, recentTask, budgetsTask);

        var expenseSummary = expenseSummaryTask.Result;
        var spentByCategory = expenseSummary.ByCategory.ToDictionary(c => c.Category, c => c.TotalAmount);

        var budgetStatuses = budgetsTask.Result
            .Select(b => ToBudgetStatus(b, spentByCategory.GetValueOrDefault(b.Category)))
            .ToList();

        var recentTransactions = recentTask.Result
            .Select(e => new RecentTransactionDto(e.Id, e.Description, e.Category, e.PaymentMethod, e.Amount, e.TransactionDate))
            .ToList();

        // No Income service exists yet; this is a configurable placeholder (Demo:MonthlyIncome) until one is added.
        var totalIncome = configuration.GetValue("Demo:MonthlyIncome", 5500m);

        return new DashboardSummaryResponse(
            periodStart, periodEnd, "USD", totalIncome, expenseSummary.TotalAmount, recentTransactions, budgetStatuses);
    }

    private static BudgetStatusDto ToBudgetStatus(BudgetResponse budget, decimal spent)
    {
        var percentUsed = budget.LimitAmount == 0 ? 0 : decimal.Round(spent / budget.LimitAmount * 100m, 2);
        var alertLevel = percentUsed switch
        {
            >= 100m => BudgetAlertLevel.Exceeded,
            _ when percentUsed >= budget.WarningThresholdPercent => BudgetAlertLevel.Warning,
            _ => BudgetAlertLevel.None,
        };

        return new BudgetStatusDto(budget.Id, budget.Category, budget.Period, budget.LimitAmount, spent, percentUsed, alertLevel);
    }
}
