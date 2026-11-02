using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PFP.Budget.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "budget");

            migrationBuilder.CreateTable(
                name: "Budgets",
                schema: "budget",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Period = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    LimitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WarningThresholdPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastAlertLevel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    LastAlertPeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.Id);
                    table.CheckConstraint("CK_Budgets_Limit_Positive", "[LimitAmount] > 0");
                    table.CheckConstraint("CK_Budgets_Threshold_Range", "[WarningThresholdPercent] > 0 AND [WarningThresholdPercent] < 100");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_UserId_IsActive",
                schema: "budget",
                table: "Budgets",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_Budgets_UserId_Category_Period_Active",
                schema: "budget",
                table: "Budgets",
                columns: new[] { "UserId", "Category", "Period" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Budgets",
                schema: "budget");
        }
    }
}
