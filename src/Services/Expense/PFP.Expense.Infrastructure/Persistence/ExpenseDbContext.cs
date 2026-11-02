using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ExpenseEntity = PFP.Expense.Domain.Entities.Expense;

namespace PFP.Expense.Infrastructure.Persistence;

/// <summary>Owns the "expense" schema. No other service reads or writes these tables directly.</summary>
public class ExpenseDbContext(DbContextOptions<ExpenseDbContext> options) : DbContext(options)
{
    public const string Schema = "expense";

    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
