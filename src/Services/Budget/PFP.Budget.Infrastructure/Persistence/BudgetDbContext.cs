using System.Reflection;
using Microsoft.EntityFrameworkCore;
using BudgetEntity = PFP.Budget.Domain.Entities.Budget;

namespace PFP.Budget.Infrastructure.Persistence;

/// <summary>Owns the "budget" schema. No other service reads or writes these tables directly.</summary>
public class BudgetDbContext(DbContextOptions<BudgetDbContext> options) : DbContext(options)
{
    public const string Schema = "budget";

    public DbSet<BudgetEntity> Budgets => Set<BudgetEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
