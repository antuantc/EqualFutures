using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EqualFutures.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountOwnerParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerParentId",
                table: "Accounts",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerParentId",
                table: "Accounts");
        }
    }
}
