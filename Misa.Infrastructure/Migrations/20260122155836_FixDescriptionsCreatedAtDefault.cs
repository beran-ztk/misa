using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDescriptionsCreatedAtDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at_utc",
                table: "descriptions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTimeOffset(new DateTime(2026, 1, 22, 16, 24, 2, 650, DateTimeKind.Unspecified).AddTicks(5698), new TimeSpan(0, 1, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at_utc",
                table: "descriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(2026, 1, 22, 16, 24, 2, 650, DateTimeKind.Unspecified).AddTicks(5698), new TimeSpan(0, 1, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");
        }
    }
}
