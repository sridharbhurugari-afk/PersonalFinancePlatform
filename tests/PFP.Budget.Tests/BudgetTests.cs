using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using PFP.Budget.Infrastructure.Persistence;
using PFP.Contracts.Common;
using PFP.Contracts.Exceptions;
using BudgetEntity = PFP.Budget.Domain.Entities.Budget;

namespace PFP.Budget.Tests;

public class BudgetEntityTests
{
    private static BudgetEntity NewBudget(BudgetPeriod period = BudgetPeriod.Monthly) =>
        BudgetEntity.Create(Guid.NewGuid(), ExpenseCategory.Dining, period, 500m, 80m, TimeProvider.System);

    [Theory]
    [InlineData(0, BudgetAlertLevel.None)]
    [InlineData(399.99, BudgetAlertLevel.None)]
    [InlineData(400, BudgetAlertLevel.Warning)]
    [InlineData(499.99, BudgetAlertLevel.Warning)]
    [InlineData(500, BudgetAlertLevel.Exceeded)]
    [InlineData(900, BudgetAlertLevel.Exceeded)]
    public void Evaluate_applies_static_thresholds(double spent, BudgetAlertLevel expected)
    {
        NewBudget().Evaluate((decimal)spent).Should().Be(expected);
    }

    [Fact]
    public void Each_threshold_is_raised_only_once_per_period()
    {
        var budget = NewBudget();
        var today = new DateOnly(2026, 11, 10);

        budget.TryRaiseAlert(300m, today).Should().BeNull();
        budget.TryRaiseAlert(410m, today).Should().Be(BudgetAlertLevel.Warning);
        budget.TryRaiseAlert(450m, today).Should().BeNull();
        budget.TryRaiseAlert(520m, today).Should().Be(BudgetAlertLevel.Exceeded);
        budget.TryRaiseAlert(600m, today).Should().BeNull();
    }

    [Fact]
    public void Alerts_reset_when_a_new_period_starts()
    {
        var budget = NewBudget();
        budget.TryRaiseAlert(450m, new DateOnly(2026, 11, 10)).Should().Be(BudgetAlertLevel.Warning);

        budget.TryRaiseAlert(450m, new DateOnly(2026, 12, 1)).Should().Be(BudgetAlertLevel.Warning);
    }

    [Fact]
    public void Weekly_period_starts_on_monday()
    {
        var budget = NewBudget(BudgetPeriod.Weekly);
        var sunday = new DateOnly(2026, 11, 8);

        budget.PeriodStart(sunday).Should().Be(new DateOnly(2026, 11, 2));
        budget.PeriodEnd(sunday).Should().Be(new DateOnly(2026, 11, 8));
    }

    [Fact]
    public void Monthly_period_spans_calendar_month()
    {
        var budget = NewBudget();

        budget.PeriodStart(new DateOnly(2026, 2, 14)).Should().Be(new DateOnly(2026, 2, 1));
        budget.PeriodEnd(new DateOnly(2026, 2, 14)).Should().Be(new DateOnly(2026, 2, 28));
    }

    [Theory]
    [InlineData(0, 80)]
    [InlineData(100, 0)]
    [InlineData(100, 100)]
    public void Create_rejects_invalid_limits(decimal limit, decimal threshold)
    {
        var act = () => BudgetEntity.Create(Guid.NewGuid(), ExpenseCategory.Other, BudgetPeriod.Monthly, limit, threshold, TimeProvider.System);

        act.Should().Throw<DomainRuleException>();
    }
}

public class BudgetDbContextModelTests
{
    [Fact]
    public void Model_declares_indexes_with_active_filter_on_unique_index()
    {
        using var context = new BudgetDbContext(
            new DbContextOptionsBuilder<BudgetDbContext>().UseSqlServer("Server=.;Database=model-only").Options);
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(BudgetEntity))!;

        entity.GetSchema().Should().Be("budget");
        entity.GetIndexes().Select(i => i.GetDatabaseName()).Should().Contain("IX_Budgets_UserId_IsActive");
        var unique = entity.GetIndexes().Single(i => i.IsUnique);
        unique.GetFilter().Should().Be("[IsActive] = 1");
    }
}
