using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PFP.Expense.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "expense");

            migrationBuilder.CreateTable(
                name: "Expenses",
                schema: "expense",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.CheckConstraint("CK_Expenses_Amount_Positive", "[Amount] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_UserId_Category_TransactionDate",
                schema: "expense",
                table: "Expenses",
                columns: new[] { "UserId", "Category", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_UserId_TransactionDate",
                schema: "expense",
                table: "Expenses",
                columns: new[] { "UserId", "TransactionDate" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Expenses",
                schema: "expense");
        }
    }
}
