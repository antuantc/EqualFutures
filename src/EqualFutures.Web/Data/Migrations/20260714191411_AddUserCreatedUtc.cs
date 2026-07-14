using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EqualFutures.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCreatedUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedUtc",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Existing accounts predate this column; backfill with "now" (the deployment
            // date) rather than leaving a misleading year-1 timestamp in the admin UI.
            migrationBuilder.Sql("UPDATE AspNetUsers SET CreatedUtc = datetime('now') WHERE CreatedUtc = '0001-01-01 00:00:00';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "AspNetUsers");
        }
    }
}
