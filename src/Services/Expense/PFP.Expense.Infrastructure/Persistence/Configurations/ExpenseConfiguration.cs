using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpenseEntity = PFP.Expense.Domain.Entities.Expense;

namespace PFP.Expense.Infrastructure.Persistence.Configurations;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<ExpenseEntity>
{
    public void Configure(EntityTypeBuilder<ExpenseEntity> builder)
    {
        builder.ToTable("Expenses", t =>
        {
            t.HasCheckConstraint("CK_Expenses_Amount_Positive", "[Amount] > 0");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        // UserId is a logical reference to the Auth service's user; deliberately not a foreign key across service databases.
        builder.Property(e => e.UserId).IsRequired();

        builder.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.PaymentMethod).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.TransactionDate).HasColumnType("date").IsRequired();
        builder.Property(e => e.Description).HasMaxLength(ExpenseEntity.DescriptionMaxLength);
        builder.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(e => e.UpdatedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
