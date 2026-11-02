using PFP.Contracts.Common;

namespace PFP.Expense.Api.Contracts;

public sealed record CreateExpenseRequest(
    decimal Amount,
    ExpenseCategory Category,
    PaymentMethod PaymentMethod,
    DateOnly TransactionDate,
    string? Description);

public sealed record UpdateExpenseRequest(
    decimal Amount,
    ExpenseCategory Category,
    PaymentMethod PaymentMethod,
    DateOnly TransactionDate,
    string? Description);

public sealed record ExpenseResponse(
    Guid Id,
    decimal Amount,
    ExpenseCategory Category,
    PaymentMethod PaymentMethod,
    DateOnly TransactionDate,
    string? Description);

/// <summary>Total spent in one category within a period; used by the Budget/Summary services to evaluate alerts.</summary>
public sealed record CategoryTotalResponse(ExpenseCategory Category, decimal TotalAmount);

public sealed record ExpensesPeriodSummaryResponse(
    DateOnly From,
    DateOnly To,
    decimal TotalAmount,
    IReadOnlyCollection<CategoryTotalResponse> ByCategory);
