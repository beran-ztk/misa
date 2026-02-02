using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Misa.Domain.Features.Messaging;

#nullable disable

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .Annotation("Npgsql:Enum:event_type", "scheduler_created_task")
                .Annotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .Annotation("Npgsql:Enum:schedule_action_type", "create_task,deadline,none,recurring")
                .Annotation("Npgsql:Enum:schedule_execution_state", "claimed,failed,pending,running,skipped,succeeded")
                .Annotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .Annotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .Annotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .Annotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .Annotation("Npgsql:Enum:session_state", "ended,paused,running")
                .Annotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .Annotation("Npgsql:Enum:workflow", "deadline,scheduling,task")
                .OldAnnotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .OldAnnotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .OldAnnotation("Npgsql:Enum:schedule_action_type", "create_task,deadline,none,recurring")
                .OldAnnotation("Npgsql:Enum:schedule_execution_state", "claimed,failed,pending,running,skipped,succeeded")
                .OldAnnotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .OldAnnotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .OldAnnotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .OldAnnotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .OldAnnotation("Npgsql:Enum:session_state", "ended,paused,running")
                .OldAnnotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .OldAnnotation("Npgsql:Enum:workflow", "deadline,scheduling,task");

            migrationBuilder.CreateTable(
                name: "outbox",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<EventType>(type: "event_type", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.EventId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .Annotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .Annotation("Npgsql:Enum:schedule_action_type", "create_task,deadline,none,recurring")
                .Annotation("Npgsql:Enum:schedule_execution_state", "claimed,failed,pending,running,skipped,succeeded")
                .Annotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .Annotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .Annotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .Annotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .Annotation("Npgsql:Enum:session_state", "ended,paused,running")
                .Annotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .Annotation("Npgsql:Enum:workflow", "deadline,scheduling,task")
                .OldAnnotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .OldAnnotation("Npgsql:Enum:event_type", "scheduler_created_task")
                .OldAnnotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .OldAnnotation("Npgsql:Enum:schedule_action_type", "create_task,deadline,none,recurring")
                .OldAnnotation("Npgsql:Enum:schedule_execution_state", "claimed,failed,pending,running,skipped,succeeded")
                .OldAnnotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .OldAnnotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .OldAnnotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .OldAnnotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .OldAnnotation("Npgsql:Enum:session_state", "ended,paused,running")
                .OldAnnotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .OldAnnotation("Npgsql:Enum:workflow", "deadline,scheduling,task");
        }
    }
}
