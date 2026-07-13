using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EqualFutures.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialFinancial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: false),
                    HouseholdName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Assumptions_InflationRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Assumptions_EducationInflationRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Assumptions_PreRetirementReturn = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Assumptions_PostRetirementReturn = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Assumptions_SafeWithdrawalRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Assumptions_PlanningHorizonAge = table.Column<int>(type: "INTEGER", nullable: false),
                    PreferredFairnessMetric = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FinancialPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    TaxTreatment = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AnnualContribution = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ExpectedReturnOverride = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    BeneficiaryChildId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Plans_FinancialPlanId",
                        column: x => x.FinancialPlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Children",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FinancialPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExpectedCollegeStartYear = table.Column<int>(type: "INTEGER", nullable: false),
                    YearsOfCollege = table.Column<int>(type: "INTEGER", nullable: false),
                    CollegeType = table.Column<int>(type: "INTEGER", nullable: false),
                    AnnualCostOverride = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    DesiredFundingPercent = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ExpectedAnnualScholarships = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Children", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Children_Plans_FinancialPlanId",
                        column: x => x.FinancialPlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Liabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FinancialPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    InterestRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MonthlyPayment = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Liabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Liabilities_Plans_FinancialPlanId",
                        column: x => x.FinancialPlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FinancialPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CurrentAge = table.Column<int>(type: "INTEGER", nullable: false),
                    PlannedRetirementAge = table.Column<int>(type: "INTEGER", nullable: false),
                    AnnualIncome = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ExpectedAnnualRetirementSpending = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    EstimatedAnnualSocialSecurity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SocialSecurityClaimingAge = table.Column<int>(type: "INTEGER", nullable: false),
                    AnnualPensionIncome = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parents_Plans_FinancialPlanId",
                        column: x => x.FinancialPlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_FinancialPlanId",
                table: "Accounts",
                column: "FinancialPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Children_FinancialPlanId",
                table: "Children",
                column: "FinancialPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Liabilities_FinancialPlanId",
                table: "Liabilities",
                column: "FinancialPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Parents_FinancialPlanId",
                table: "Parents",
                column: "FinancialPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_OwnerId",
                table: "Plans",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Children");

            migrationBuilder.DropTable(
                name: "Liabilities");

            migrationBuilder.DropTable(
                name: "Parents");

            migrationBuilder.DropTable(
                name: "Plans");
        }
    }
}
