using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EqualFutures.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveRetirementSpendingToHousehold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAnnualRetirementSpending",
                table: "Plans",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE Plans
                SET ExpectedAnnualRetirementSpending = COALESCE((
                    SELECT MAX(Parents.ExpectedAnnualRetirementSpending)
                    FROM Parents
                    WHERE Parents.FinancialPlanId = Plans.Id
                ), 0)
                """);

            migrationBuilder.DropColumn(
                name: "ExpectedAnnualRetirementSpending",
                table: "Parents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAnnualRetirementSpending",
                table: "Parents",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE Parents
                SET ExpectedAnnualRetirementSpending = COALESCE((
                    SELECT Plans.ExpectedAnnualRetirementSpending
                    FROM Plans
                    WHERE Plans.Id = Parents.FinancialPlanId
                ), 0)
                """);

            migrationBuilder.DropColumn(
                name: "ExpectedAnnualRetirementSpending",
                table: "Plans");
        }
    }
}
