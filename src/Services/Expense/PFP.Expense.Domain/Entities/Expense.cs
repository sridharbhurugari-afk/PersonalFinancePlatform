using Microsoft.EntityFrameworkCore;
using PFP.Contracts.Common;
using PFP.Contracts.Exceptions;

namespace PFP.Expense.Domain.Entities;

// Composite indexes lead with UserId (every query is tenant-scoped) and are ordered for the two hot paths:
// "recent transactions" (UserId, TransactionDate DESC) and "spend per category in a period".
[Index(nameof(UserId), nameof(TransactionDate), Name = "IX_Expenses_UserId_TransactionDate", IsDescending = [false, true])]
[Index(nameof(UserId), nameof(Category), nameof(TransactionDate), Name = "IX_Expenses_UserId_Category_TransactionDate")]
public class Expense
{
    public const int DescriptionMaxLength = 500;

    private Expense()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public decimal Amount { get; private set; }

    public ExpenseCategory Category { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public DateOnly TransactionDate { get; private set; }

    public string? Description { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static Expense Create(
        Guid userId,
        decimal amount,
        ExpenseCategory category,
        PaymentMethod paymentMethod,
        DateOnly transactionDate,
        string? description,
        TimeProvider clock)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainRuleException("An expense must belong to a user.", "expense_user_required");
        }

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime,
        };
        expense.Apply(amount, category, paymentMethod, transactionDate, description);
        return expense;
    }

    public void Update(
        decimal amount,
        ExpenseCategory category,
        PaymentMethod paymentMethod,
        DateOnly transactionDate,
        string? description,
        TimeProvider clock)
    {
        Apply(amount, category, paymentMethod, transactionDate, description);
        UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
    }

    private void Apply(decimal amount, ExpenseCategory category, PaymentMethod paymentMethod, DateOnly date, string? description)
    {
        if (amount <= 0)
        {
            throw new DomainRuleException("Expense amount must be greater than zero.", "expense_amount_invalid");
        }

        if (description?.Length > DescriptionMaxLength)
        {
            throw new DomainRuleException($"Description cannot exceed {DescriptionMaxLength} characters.", "expense_description_too_long");
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
        Category = category;
        PaymentMethod = paymentMethod;
        TransactionDate = date;
        Description = description?.Trim();
    }
}
