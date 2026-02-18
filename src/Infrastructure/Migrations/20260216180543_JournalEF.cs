using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Misa.Domain.Features.Audit;

#nullable disable

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class JournalEF : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .Annotation("Npgsql:Enum:event_type", "scheduler_created_task")
                .Annotation("Npgsql:Enum:journal_system_type", "session")
                .Annotation("Npgsql:Enum:outbox_event_state", "pending,processed")
                .Annotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .Annotation("Npgsql:Enum:schedule_action_type", "create_task,none,recurring")
                .Annotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .Annotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .Annotation("Npgsql:Enum:scheduler_execution_status", "claimed,failed,pending,running,skipped,succeeded")
                .Annotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .Annotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .Annotation("Npgsql:Enum:session_state", "ended,paused,running")
                .Annotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .Annotation("Npgsql:Enum:workflow", "deadline,scheduling,task")
                .OldAnnotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .OldAnnotation("Npgsql:Enum:event_type", "scheduler_created_task")
                .OldAnnotation("Npgsql:Enum:outbox_event_state", "pending,processed")
                .OldAnnotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .OldAnnotation("Npgsql:Enum:schedule_action_type", "create_task,none,recurring")
                .OldAnnotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .OldAnnotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .OldAnnotation("Npgsql:Enum:scheduler_execution_status", "claimed,failed,pending,running,skipped,succeeded")
                .OldAnnotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .OldAnnotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .OldAnnotation("Npgsql:Enum:session_state", "ended,paused,running")
                .OldAnnotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .OldAnnotation("Npgsql:Enum:workflow", "deadline,scheduling,task");

            migrationBuilder.CreateTable(
                name: "journals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "journal_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_journal_categories_journals_JournalId",
                        column: x => x.JournalId,
                        principalTable: "journals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UntilAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    OriginId = table.Column<Guid>(type: "uuid", nullable: true),
                    SystemType = table.Column<JournalSystemType>(type: "journal_system_type", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_journal_entries_journals_JournalId",
                        column: x => x.JournalId,
                        principalTable: "journals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_journal_categories_JournalId_Name",
                table: "journal_categories",
                columns: new[] { "JournalId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_JournalId",
                table: "journal_entries",
                column: "JournalId");

            migrationBuilder.CreateIndex(
                name: "IX_journals_UserId",
                table: "journals",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "journal_categories");

            migrationBuilder.DropTable(
                name: "journal_entries");

            migrationBuilder.DropTable(
                name: "journals");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .Annotation("Npgsql:Enum:event_type", "scheduler_created_task")
                .Annotation("Npgsql:Enum:outbox_event_state", "pending,processed")
                .Annotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .Annotation("Npgsql:Enum:schedule_action_type", "create_task,none,recurring")
                .Annotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .Annotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .Annotation("Npgsql:Enum:scheduler_execution_status", "claimed,failed,pending,running,skipped,succeeded")
                .Annotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .Annotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .Annotation("Npgsql:Enum:session_state", "ended,paused,running")
                .Annotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .Annotation("Npgsql:Enum:workflow", "deadline,scheduling,task")
                .OldAnnotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .OldAnnotation("Npgsql:Enum:event_type", "scheduler_created_task")
                .OldAnnotation("Npgsql:Enum:journal_system_type", "session")
                .OldAnnotation("Npgsql:Enum:outbox_event_state", "pending,processed")
                .OldAnnotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .OldAnnotation("Npgsql:Enum:schedule_action_type", "create_task,none,recurring")
                .OldAnnotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .OldAnnotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .OldAnnotation("Npgsql:Enum:scheduler_execution_status", "claimed,failed,pending,running,skipped,succeeded")
                .OldAnnotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .OldAnnotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .OldAnnotation("Npgsql:Enum:session_state", "ended,paused,running")
                .OldAnnotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .OldAnnotation("Npgsql:Enum:workflow", "deadline,scheduling,task");
        }
    }
}
