using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniqueExecutionLogWithScheduledFor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_scheduler_execution_log_scheduler_id",
                table: "scheduler_execution_log");

            migrationBuilder.CreateIndex(
                name: "IX_scheduler_execution_log_scheduler_id_scheduled_for_utc",
                table: "scheduler_execution_log",
                columns: new[] { "scheduler_id", "scheduled_for_utc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_scheduler_execution_log_scheduler_id_scheduled_for_utc",
                table: "scheduler_execution_log");

            migrationBuilder.CreateIndex(
                name: "IX_scheduler_execution_log_scheduler_id",
                table: "scheduler_execution_log",
                column: "scheduler_id");
        }
    }
}
