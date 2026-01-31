using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

#nullable disable

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulerActionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_deadlines");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .Annotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .Annotation("Npgsql:Enum:schedule_action_type", "create_item,deadline,recurring")
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
                .OldAnnotation("Npgsql:Enum:schedule_execution_state", "claimed,failed,pending,running,skipped,succeeded")
                .OldAnnotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .OldAnnotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .OldAnnotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .OldAnnotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .OldAnnotation("Npgsql:Enum:session_state", "ended,paused,running")
                .OldAnnotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .OldAnnotation("Npgsql:Enum:workflow", "deadline,scheduling,task");

            migrationBuilder.AddColumn<ScheduleActionType>(
                name: "action_type",
                table: "scheduler",
                type: "schedule_action_type",
                nullable: false,
                defaultValue: ScheduleActionType.Deadline);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "action_type",
                table: "scheduler");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .Annotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
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
                .OldAnnotation("Npgsql:Enum:schedule_action_type", "create_item,deadline,recurring")
                .OldAnnotation("Npgsql:Enum:schedule_execution_state", "claimed,failed,pending,running,skipped,succeeded")
                .OldAnnotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .OldAnnotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .OldAnnotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .OldAnnotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .OldAnnotation("Npgsql:Enum:session_state", "ended,paused,running")
                .OldAnnotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .OldAnnotation("Npgsql:Enum:workflow", "deadline,scheduling,task");

            migrationBuilder.CreateTable(
                name: "scheduled_deadlines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    deadline_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_deadlines", x => x.id);
                    table.ForeignKey(
                        name: "FK_scheduled_deadlines_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_deadlines_item_id",
                table: "scheduled_deadlines",
                column: "item_id",
                unique: true);
        }
    }
}
