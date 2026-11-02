using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NSubstitute;
using PFP.Contracts.Common;
using PFP.Contracts.Exceptions;
using PFP.Expense.Infrastructure.Persistence;
using ExpenseEntity = PFP.Expense.Domain.Entities.Expense;

namespace PFP.Expense.Tests;

public class ExpenseEntityTests
{
    private static readonly TimeProvider Clock = TimeProvider.System;

    private static ExpenseEntity NewExpense(decimal amount = 12.345m) => ExpenseEntity.Create(
        Guid.NewGuid(), amount, ExpenseCategory.Groceries, PaymentMethod.DebitCard, new DateOnly(2026, 11, 1), "  Weekly shop ", Clock);

    [Fact]
    public void Create_normalises_amount_and_description()
    {
        var expense = NewExpense();

        expense.Amount.Should().Be(12.34m);
        expense.Description.Should().Be("Weekly shop");
        expense.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_rejects_non_positive_amount(decimal amount)
    {
        var act = () => NewExpense(amount);

        act.Should().Throw<DomainRuleException>().Which.ErrorCode.Should().Be("expense_amount_invalid");
    }

    [Fact]
    public void Update_stamps_modified_time_from_clock()
    {
        var fixedNow = new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(fixedNow);
        var expense = NewExpense();

        expense.Update(20m, ExpenseCategory.Dining, PaymentMethod.Cash, new DateOnly(2026, 11, 2), null, clock);

        expense.UpdatedAtUtc.Should().Be(fixedNow.UtcDateTime);
        expense.Category.Should().Be(ExpenseCategory.Dining);
    }
}

public class ExpenseDbContextModelTests
{
    // Building the model does not open a connection, so no SQL Server is needed.
    private static ExpenseDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ExpenseDbContext>().UseSqlServer("Server=.;Database=model-only").Options);

    [Fact]
    public void Model_declares_query_path_indexes()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ExpenseEntity))!;

        var indexes = entity.GetIndexes().Select(i => i.GetDatabaseName()).ToList();

        indexes.Should().Contain("IX_Expenses_UserId_TransactionDate");
        indexes.Should().Contain("IX_Expenses_UserId_Category_TransactionDate");
    }

    [Fact]
    public void Model_uses_expense_schema_and_decimal_precision()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ExpenseEntity))!;

        entity.GetSchema().Should().Be("expense");
        entity.FindProperty(nameof(ExpenseEntity.Amount))!.GetPrecision().Should().Be(18);
        entity.FindProperty(nameof(ExpenseEntity.Amount))!.GetScale().Should().Be(2);
        entity.FindProperty(nameof(ExpenseEntity.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
    }
}
