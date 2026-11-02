using Microsoft.EntityFrameworkCore;
using PFP.Contracts.Common;
using PFP.Expense.Infrastructure.Persistence;
using ExpenseEntity = PFP.Expense.Domain.Entities.Expense;

namespace PFP.Expense.Api.Persistence;

/// <summary>Seeds a few months of demo expenses for <see cref="DemoUser"/> so the dashboard has data to show
/// before the Auth service and real data entry exist. Runs once at startup in Development only; no-op if any row exists.</summary>
public static class ExpenseDbSeeder
{
    public static async Task SeedAsync(ExpenseDbContext db, TimeProvider clock, CancellationToken ct = default)
    {
        if (await db.Expenses.AnyAsync(ct))
        {
            return;
        }

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var random = new Random(42); // fixed seed: stable demo data across restarts
        var categories = new[]
        {
            (ExpenseCategory.Groceries, 40m, 120m),
            (ExpenseCategory.Dining, 15m, 90m),
            (ExpenseCategory.Transportation, 20m, 60m),
            (ExpenseCategory.Utilities, 50m, 150m),
            (ExpenseCategory.Entertainment, 10m, 70m),
        };

        var expenses = new List<ExpenseEntity>();
        for (var daysAgo = 0; daysAgo < 60; daysAgo++)
        {
            if (random.NextDouble() > 0.55)
            {
                continue; // not every day has a transaction
            }

            var (category, min, max) = categories[random.Next(categories.Length)];
            var amount = Math.Round((decimal)random.NextDouble() * (max - min) + min, 2);
            var paymentMethod = (PaymentMethod)random.Next(Enum.GetValues<PaymentMethod>().Length);

            expenses.Add(ExpenseEntity.Create(
                DemoUser.Id, amount, category, paymentMethod, today.AddDays(-daysAgo), $"Demo {category} expense", clock));
        }

        db.Expenses.AddRange(expenses);
        await db.SaveChangesAsync(ct);
    }
}
