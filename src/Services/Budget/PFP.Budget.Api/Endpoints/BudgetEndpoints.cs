using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PFP.Budget.Api.Contracts;
using PFP.Budget.Infrastructure.Persistence;
using PFP.Contracts.Common;
using BudgetEntity = PFP.Budget.Domain.Entities.Budget;

namespace PFP.Budget.Api.Endpoints;

public static class BudgetEndpoints
{
    public static RouteGroupBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/budgets").WithTags("Budgets");

        group.MapGet("/", GetAllAsync).WithName("GetBudgets");
        group.MapGet("/{id:guid}", GetByIdAsync).WithName("GetBudgetById");
        group.MapPost("/", CreateAsync).WithName("CreateBudget");
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateBudget");
        group.MapDelete("/{id:guid}", DeactivateAsync).WithName("DeactivateBudget");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<BudgetResponse>>> GetAllAsync(
        BudgetDbContext db, Guid? userId, bool activeOnly = true, CancellationToken ct = default)
    {
        var resolvedUserId = userId ?? DemoUser.Id;
        var query = db.Budgets.Where(b => b.UserId == resolvedUserId);
        if (activeOnly)
        {
            query = query.Where(b => b.IsActive);
        }

        var budgets = await query.ToListAsync(ct);
        return TypedResults.Ok<IReadOnlyCollection<BudgetResponse>>(budgets.Select(ToResponse).ToList());
    }

    private static async Task<Results<Ok<BudgetResponse>, NotFound>> GetByIdAsync(BudgetDbContext db, Guid id, CancellationToken ct)
    {
        var budget = await db.Budgets.FindAsync([id], ct);
        return budget is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(budget));
    }

    private static async Task<Results<Created<BudgetResponse>, ValidationProblem>> CreateAsync(
        BudgetDbContext db,
        TimeProvider clock,
        IValidator<CreateBudgetRequest> validator,
        CreateBudgetRequest request,
        Guid? userId,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var budget = BudgetEntity.Create(
            userId ?? DemoUser.Id, request.Category, request.Period, request.LimitAmount, request.WarningThresholdPercent, clock);

        db.Budgets.Add(budget);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/budgets/{budget.Id}", ToResponse(budget));
    }

    private static async Task<Results<Ok<BudgetResponse>, NotFound, ValidationProblem>> UpdateAsync(
        BudgetDbContext db,
        TimeProvider clock,
        IValidator<UpdateBudgetRequest> validator,
        Guid id,
        UpdateBudgetRequest request,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var budget = await db.Budgets.FindAsync([id], ct);
        if (budget is null)
        {
            return TypedResults.NotFound();
        }

        budget.UpdateLimits(request.LimitAmount, request.WarningThresholdPercent, clock);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ToResponse(budget));
    }

    private static async Task<Results<NoContent, NotFound>> DeactivateAsync(BudgetDbContext db, TimeProvider clock, Guid id, CancellationToken ct)
    {
        var budget = await db.Budgets.FindAsync([id], ct);
        if (budget is null)
        {
            return TypedResults.NotFound();
        }

        budget.Deactivate(clock);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static BudgetResponse ToResponse(BudgetEntity b) =>
        new(b.Id, b.Category, b.Period, b.LimitAmount, b.WarningThresholdPercent, b.IsActive);
}
