using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EqualFutures.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AllowNewRegistrations = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultInflationRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DefaultEducationInflationRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DefaultPreRetirementReturn = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DefaultPostRetirementReturn = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DefaultRealEstateAppreciationRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DefaultSafeWithdrawalRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DefaultPlanningHorizonAge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");
        }
    }
}
