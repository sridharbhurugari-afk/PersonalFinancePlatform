using Microsoft.EntityFrameworkCore;
using PFP.Budget.Infrastructure.Persistence;
using PFP.Contracts.Common;
using BudgetEntity = PFP.Budget.Domain.Entities.Budget;

namespace PFP.Budget.Api.Persistence;

/// <summary>Seeds a handful of active demo budgets for <see cref="DemoUser"/>, mirroring the categories the
/// Expense service seeds. Runs once at startup in Development only; no-op if any row exists.</summary>
public static class BudgetDbSeeder
{
    public static async Task SeedAsync(BudgetDbContext db, TimeProvider clock, CancellationToken ct = default)
    {
        if (await db.Budgets.AnyAsync(ct))
        {
            return;
        }

        var budgets = new[]
        {
            BudgetEntity.Create(DemoUser.Id, ExpenseCategory.Groceries, BudgetPeriod.Monthly, 400m, 80m, clock),
            BudgetEntity.Create(DemoUser.Id, ExpenseCategory.Dining, BudgetPeriod.Monthly, 200m, 80m, clock),
            BudgetEntity.Create(DemoUser.Id, ExpenseCategory.Transportation, BudgetPeriod.Monthly, 150m, 80m, clock),
            BudgetEntity.Create(DemoUser.Id, ExpenseCategory.Entertainment, BudgetPeriod.Monthly, 100m, 80m, clock),
        };

        db.Budgets.AddRange(budgets);
        await db.SaveChangesAsync(ct);
    }
}
