using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PFP.Contracts.Common;
using PFP.Contracts.Exceptions;
using PFP.Expense.Api.Contracts;
using PFP.Expense.Infrastructure.Persistence;
using ExpenseEntity = PFP.Expense.Domain.Entities.Expense;

namespace PFP.Expense.Api.Endpoints;

public static class ExpenseEndpoints
{
    public static RouteGroupBuilder MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/expenses").WithTags("Expenses");

        group.MapGet("/", GetRecentAsync).WithName("GetRecentExpenses");
        group.MapGet("/{id:guid}", GetByIdAsync).WithName("GetExpenseById");
        group.MapPost("/", CreateAsync).WithName("CreateExpense");
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateExpense");
        group.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteExpense");
        group.MapGet("/summary", GetPeriodSummaryAsync).WithName("GetExpensesPeriodSummary");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<ExpenseResponse>>> GetRecentAsync(
        ExpenseDbContext db, Guid? userId, int take = 10, CancellationToken ct = default)
    {
        var resolvedUserId = userId ?? DemoUser.Id;
        var expenses = await db.Expenses
            .Where(e => e.UserId == resolvedUserId)
            .OrderByDescending(e => e.TransactionDate)
            .ThenByDescending(e => e.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);

        return TypedResults.Ok<IReadOnlyCollection<ExpenseResponse>>(expenses.Select(ToResponse).ToList());
    }

    private static async Task<Results<Ok<ExpenseResponse>, NotFound>> GetByIdAsync(
        ExpenseDbContext db, Guid id, CancellationToken ct)
    {
        var expense = await db.Expenses.FindAsync([id], ct);
        return expense is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToResponse(expense));
    }

    private static async Task<Results<Created<ExpenseResponse>, ValidationProblem>> CreateAsync(
        ExpenseDbContext db,
        TimeProvider clock,
        IValidator<CreateExpenseRequest> validator,
        CreateExpenseRequest request,
        Guid? userId,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var expense = ExpenseEntity.Create(
            userId ?? DemoUser.Id, request.Amount, request.Category, request.PaymentMethod, request.TransactionDate, request.Description, clock);

        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/expenses/{expense.Id}", ToResponse(expense));
    }

    private static async Task<Results<Ok<ExpenseResponse>, NotFound, ValidationProblem>> UpdateAsync(
        ExpenseDbContext db,
        TimeProvider clock,
        IValidator<UpdateExpenseRequest> validator,
        Guid id,
        UpdateExpenseRequest request,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var expense = await db.Expenses.FindAsync([id], ct);
        if (expense is null)
        {
            return TypedResults.NotFound();
        }

        expense.Update(request.Amount, request.Category, request.PaymentMethod, request.TransactionDate, request.Description, clock);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ToResponse(expense));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(ExpenseDbContext db, Guid id, CancellationToken ct)
    {
        var expense = await db.Expenses.FindAsync([id], ct);
        if (expense is null)
        {
            return TypedResults.NotFound();
        }

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<ExpensesPeriodSummaryResponse>, ValidationProblem>> GetPeriodSummaryAsync(
        ExpenseDbContext db, Guid? userId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (to < from)
        {
            throw new DomainRuleException("'to' must not be before 'from'.", "invalid_date_range");
        }

        var resolvedUserId = userId ?? DemoUser.Id;
        var byCategory = await db.Expenses
            .Where(e => e.UserId == resolvedUserId && e.TransactionDate >= from && e.TransactionDate <= to)
            .GroupBy(e => e.Category)
            .Select(g => new CategoryTotalResponse(g.Key, g.Sum(e => e.Amount)))
            .ToListAsync(ct);

        return TypedResults.Ok(new ExpensesPeriodSummaryResponse(from, to, byCategory.Sum(c => c.TotalAmount), byCategory));
    }

    private static ExpenseResponse ToResponse(ExpenseEntity e) =>
        new(e.Id, e.Amount, e.Category, e.PaymentMethod, e.TransactionDate, e.Description);
}
