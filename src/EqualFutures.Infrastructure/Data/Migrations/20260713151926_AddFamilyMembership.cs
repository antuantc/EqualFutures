using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EqualFutures.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FinancialPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    InvitedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AcceptedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    AcceptedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanInvitations_Plans_FinancialPlanId",
                        column: x => x.FinancialPlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FinancialPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    JoinedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanMembers_Plans_FinancialPlanId",
                        column: x => x.FinancialPlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanInvitations_Email",
                table: "PlanInvitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_PlanInvitations_FinancialPlanId",
                table: "PlanInvitations",
                column: "FinancialPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanInvitations_Token",
                table: "PlanInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanMembers_FinancialPlanId_UserId",
                table: "PlanMembers",
                columns: new[] { "FinancialPlanId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanMembers_UserId",
                table: "PlanMembers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanInvitations");

            migrationBuilder.DropTable(
                name: "PlanMembers");
        }
    }
}
