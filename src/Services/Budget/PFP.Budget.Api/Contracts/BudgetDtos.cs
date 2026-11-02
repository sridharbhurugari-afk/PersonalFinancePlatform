using PFP.Contracts.Common;

namespace PFP.Budget.Api.Contracts;

public sealed record CreateBudgetRequest(
    ExpenseCategory Category,
    BudgetPeriod Period,
    decimal LimitAmount,
    decimal WarningThresholdPercent);

public sealed record UpdateBudgetRequest(decimal LimitAmount, decimal WarningThresholdPercent);

public sealed record BudgetResponse(
    Guid Id,
    ExpenseCategory Category,
    BudgetPeriod Period,
    decimal LimitAmount,
    decimal WarningThresholdPercent,
    bool IsActive);
