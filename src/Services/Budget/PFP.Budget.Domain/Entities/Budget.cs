using Microsoft.EntityFrameworkCore;
using PFP.Contracts.Common;
using PFP.Contracts.Exceptions;

namespace PFP.Budget.Domain.Entities;

// IX_Budgets_UserId_IsActive serves "my active budgets" (dashboard). The unique filtered index guarantees at
// most one active budget per user/category/period, which also makes alert evaluation unambiguous.
[Index(nameof(UserId), nameof(IsActive), Name = "IX_Budgets_UserId_IsActive")]
[Index(nameof(UserId), nameof(Category), nameof(Period), IsUnique = true, Name = "UX_Budgets_UserId_Category_Period_Active")]
public class Budget
{
    public const decimal DefaultWarningPercent = 80m;

    private Budget()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public ExpenseCategory Category { get; private set; }

    public BudgetPeriod Period { get; private set; }

    public decimal LimitAmount { get; private set; }

    /// <summary>Percentage of <see cref="LimitAmount"/> at which a warning is raised (the 100% breach is fixed).</summary>
    public decimal WarningThresholdPercent { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Highest alert already published for the current period; prevents duplicate notifications.</summary>
    public BudgetAlertLevel LastAlertLevel { get; private set; }

    public DateOnly? LastAlertPeriodStart { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static Budget Create(
        Guid userId,
        ExpenseCategory category,
        BudgetPeriod period,
        decimal limitAmount,
        decimal warningThresholdPercent,
        TimeProvider clock)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainRuleException("A budget must belong to a user.", "budget_user_required");
        }

        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = category,
            Period = period,
            IsActive = true,
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime,
        };
        budget.SetLimits(limitAmount, warningThresholdPercent);
        return budget;
    }

    public void UpdateLimits(decimal limitAmount, decimal warningThresholdPercent, TimeProvider clock)
    {
        SetLimits(limitAmount, warningThresholdPercent);
        UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
    }

    public void Deactivate(TimeProvider clock)
    {
        IsActive = false;
        UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
    }

    /// <summary>Start of the budget window (Monday for weekly, first day for monthly) that contains <paramref name="date"/>.</summary>
    public DateOnly PeriodStart(DateOnly date) => Period switch
    {
        BudgetPeriod.Weekly => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
        _ => new DateOnly(date.Year, date.Month, 1),
    };

    public DateOnly PeriodEnd(DateOnly date) => Period switch
    {
        BudgetPeriod.Weekly => PeriodStart(date).AddDays(6),
        _ => PeriodStart(date).AddMonths(1).AddDays(-1),
    };

    public decimal PercentUsed(decimal spent) => decimal.Round(spent / LimitAmount * 100m, 2);

    /// <summary>Static threshold evaluation: Warning at the configured percent, Exceeded at 100%.</summary>
    public BudgetAlertLevel Evaluate(decimal spent)
    {
        // Compare the unrounded ratio so 99.998% is never reported as 100%.
        var percent = spent / LimitAmount * 100m;
        if (percent >= 100m)
        {
            return BudgetAlertLevel.Exceeded;
        }

        return percent >= WarningThresholdPercent ? BudgetAlertLevel.Warning : BudgetAlertLevel.None;
    }

    /// <summary>
    /// Returns the new alert level only when it is higher than the one already raised in this period,
    /// so each threshold fires once per period. Records the level as a side effect.
    /// </summary>
    public BudgetAlertLevel? TryRaiseAlert(decimal spent, DateOnly today)
    {
        var periodStart = PeriodStart(today);
        if (LastAlertPeriodStart != periodStart)
        {
            LastAlertPeriodStart = periodStart;
            LastAlertLevel = BudgetAlertLevel.None;
        }

        var level = Evaluate(spent);
        if (level <= LastAlertLevel)
        {
            return null;
        }

        LastAlertLevel = level;
        return level;
    }

    private void SetLimits(decimal limitAmount, decimal warningThresholdPercent)
    {
        if (limitAmount <= 0)
        {
            throw new DomainRuleException("Budget limit must be greater than zero.", "budget_limit_invalid");
        }

        if (warningThresholdPercent is <= 0 or >= 100)
        {
            throw new DomainRuleException("Warning threshold must be between 0 and 100 (exclusive).", "budget_threshold_invalid");
        }

        LimitAmount = decimal.Round(limitAmount, 2);
        WarningThresholdPercent = warningThresholdPercent;
    }
}
