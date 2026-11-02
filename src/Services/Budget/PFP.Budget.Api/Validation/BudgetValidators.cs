using FluentValidation;
using PFP.Budget.Api.Contracts;

namespace PFP.Budget.Api.Validation;

public sealed class CreateBudgetRequestValidator : AbstractValidator<CreateBudgetRequest>
{
    public CreateBudgetRequestValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Period).IsInEnum();
        RuleFor(x => x.LimitAmount).GreaterThan(0);
        RuleFor(x => x.WarningThresholdPercent).ExclusiveBetween(0, 100);
    }
}

public sealed class UpdateBudgetRequestValidator : AbstractValidator<UpdateBudgetRequest>
{
    public UpdateBudgetRequestValidator()
    {
        RuleFor(x => x.LimitAmount).GreaterThan(0);
        RuleFor(x => x.WarningThresholdPercent).ExclusiveBetween(0, 100);
    }
}
