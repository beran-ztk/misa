using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class occurenceCanBeZero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_scheduler_occurrence_count_limit_ge_1",
                table: "Schedulers");

            migrationBuilder.AddCheckConstraint(
                name: "ck_scheduler_occurrence_count_limit_ge_1",
                table: "Schedulers",
                sql: "\"OccurrenceCountLimit\" IS NULL OR \"OccurrenceCountLimit\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_scheduler_occurrence_count_limit_ge_1",
                table: "Schedulers");

            migrationBuilder.AddCheckConstraint(
                name: "ck_scheduler_occurrence_count_limit_ge_1",
                table: "Schedulers",
                sql: "\"OccurrenceCountLimit\" IS NULL OR \"OccurrenceCountLimit\" >= 1");
        }
    }
}
