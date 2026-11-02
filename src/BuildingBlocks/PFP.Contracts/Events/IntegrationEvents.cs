using PFP.Contracts.Common;

namespace PFP.Contracts.Events;

/// <summary>Base shape for every integration event published on the bus.</summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}

/// <summary>Published by the Expense service; consumed by the Budget service to re-evaluate thresholds.</summary>
public sealed record ExpenseRecorded(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid ExpenseId,
    Guid UserId,
    ExpenseCategory Category,
    decimal Amount,
    DateOnly TransactionDate) : IIntegrationEvent;

/// <summary>Published by the Budget service when spending crosses a warning/exceeded threshold.</summary>
public sealed record BudgetThresholdBreached(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid BudgetId,
    Guid UserId,
    ExpenseCategory Category,
    BudgetAlertLevel Level,
    decimal LimitAmount,
    decimal SpentAmount,
    decimal PercentUsed) : IIntegrationEvent;
