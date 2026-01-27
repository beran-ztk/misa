using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Nextallowedexecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "lookahead_count",
                table: "scheduler",
                newName: "LookaheadCount");

            migrationBuilder.AlterColumn<int>(
                name: "LookaheadCount",
                table: "scheduler",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "lookahead_limit",
                table: "scheduler",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_allowed_execution_at_utc",
                table: "scheduler",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lookahead_limit",
                table: "scheduler");

            migrationBuilder.DropColumn(
                name: "next_allowed_execution_at_utc",
                table: "scheduler");

            migrationBuilder.RenameColumn(
                name: "LookaheadCount",
                table: "scheduler",
                newName: "lookahead_count");

            migrationBuilder.AlterColumn<int>(
                name: "lookahead_count",
                table: "scheduler",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
