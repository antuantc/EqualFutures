using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EqualFutures.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRealEstateDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Assumptions_RealEstateAppreciationRate",
                table: "Plans",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0.035m);

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualCapExReservePercent",
                table: "Accounts",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyOperatingExpenses",
                table: "Accounts",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyRentToday",
                table: "Accounts",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecuredByLiabilityId",
                table: "Accounts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Use",
                table: "Accounts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VacancyRate",
                table: "Accounts",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Assumptions_RealEstateAppreciationRate",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "AnnualCapExReservePercent",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "MonthlyOperatingExpenses",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "MonthlyRentToday",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SecuredByLiabilityId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Use",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "VacancyRate",
                table: "Accounts");
        }
    }
}
