using FluentValidation;
using PFP.Expense.Api.Contracts;

namespace PFP.Expense.Api.Validation;

public sealed class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.TransactionDate).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.Date));
        RuleFor(x => x.Description).MaximumLength(Domain.Entities.Expense.DescriptionMaxLength);
    }
}

public sealed class UpdateExpenseRequestValidator : AbstractValidator<UpdateExpenseRequest>
{
    public UpdateExpenseRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.TransactionDate).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.Date));
        RuleFor(x => x.Description).MaximumLength(Domain.Entities.Expense.DescriptionMaxLength);
    }
}
