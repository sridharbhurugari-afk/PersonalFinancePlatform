using PFP.Contracts.Common;

namespace PFP.Summary.Api.Contracts;

// Mirrors the Expense service's public response shape. Owned separately per microservice boundaries even
// though it currently matches PFP.Expense.Api.Contracts.ExpenseResponse field-for-field.
public sealed record ExpenseResponse(
    Guid Id,
    decimal Amount,
    ExpenseCategory Category,
    PaymentMethod PaymentMethod,
    DateOnly TransactionDate,
    string? Description);

public sealed record CategoryTotalResponse(ExpenseCategory Category, decimal TotalAmount);

public sealed record ExpensesPeriodSummaryResponse(
    DateOnly From,
    DateOnly To,
    decimal TotalAmount,
    IReadOnlyCollection<CategoryTotalResponse> ByCategory);

// Mirrors the Budget service's public response shape.
public sealed record BudgetResponse(
    Guid Id,
    ExpenseCategory Category,
    BudgetPeriod Period,
    decimal LimitAmount,
    decimal WarningThresholdPercent,
    bool IsActive);

public sealed record RecentTransactionDto(
    Guid Id,
    string? Description,
    ExpenseCategory Category,
    PaymentMethod PaymentMethod,
    decimal Amount,
    DateOnly TransactionDate);

public sealed record BudgetStatusDto(
    Guid BudgetId,
    ExpenseCategory Category,
    BudgetPeriod Period,
    decimal LimitAmount,
    decimal SpentAmount,
    decimal PercentUsed,
    BudgetAlertLevel AlertLevel);

/// <summary>Shape consumed directly by the Angular dashboard (see finance-web's DashboardSummary model).</summary>
public sealed record DashboardSummaryResponse(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Currency,
    decimal TotalIncome,
    decimal TotalExpenses,
    IReadOnlyCollection<RecentTransactionDto> RecentTransactions,
    IReadOnlyCollection<BudgetStatusDto> Budgets);
