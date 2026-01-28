using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RmLookaheadcount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LookaheadCount",
                table: "scheduler");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LookaheadCount",
                table: "scheduler",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
