using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BudgetEntity = PFP.Budget.Domain.Entities.Budget;

namespace PFP.Budget.Infrastructure.Persistence.Configurations;

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<BudgetEntity>
{
    public void Configure(EntityTypeBuilder<BudgetEntity> builder)
    {
        builder.ToTable("Budgets", t =>
        {
            t.HasCheckConstraint("CK_Budgets_Limit_Positive", "[LimitAmount] > 0");
            t.HasCheckConstraint("CK_Budgets_Threshold_Range", "[WarningThresholdPercent] > 0 AND [WarningThresholdPercent] < 100");
        });

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.UserId).IsRequired();
        builder.Property(b => b.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(b => b.Period).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(b => b.LimitAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(b => b.WarningThresholdPercent).HasPrecision(5, 2).IsRequired();
        builder.Property(b => b.LastAlertLevel).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(b => b.LastAlertPeriodStart).HasColumnType("date");
        builder.Property(b => b.CreatedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(b => b.UpdatedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(b => b.RowVersion).IsRowVersion();

        // The [Index] attribute declares the unique index; this adds the filter (attributes cannot) so deactivated
        // budgets can be archived without blocking a new one. Same name => same index.
        builder.HasIndex(new[] { nameof(BudgetEntity.UserId), nameof(BudgetEntity.Category), nameof(BudgetEntity.Period) }, "UX_Budgets_UserId_Category_Period_Active")
            .IsUnique()
            .HasFilter("[IsActive] = 1");
    }
}
