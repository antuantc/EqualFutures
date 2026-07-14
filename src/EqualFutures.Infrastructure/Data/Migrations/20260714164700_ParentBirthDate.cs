using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EqualFutures.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ParentBirthDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "Parents",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // Best-effort backfill: derive a birth date from the previous CurrentAge so
            // existing plans don't suddenly show a parent born in year 1.
            migrationBuilder.Sql(
                "UPDATE Parents SET BirthDate = (CAST(strftime('%Y','now') AS INTEGER) - CurrentAge) || '-01-01';");

            migrationBuilder.DropColumn(
                name: "CurrentAge",
                table: "Parents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentAge",
                table: "Parents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE Parents SET CurrentAge = CAST(strftime('%Y','now') AS INTEGER) - CAST(strftime('%Y', BirthDate) AS INTEGER);");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Parents");
        }
    }
}
