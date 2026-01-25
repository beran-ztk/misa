using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSchedulerItemIdAndUseSharedPk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scheduler_items_item_id",
                table: "scheduler");

            migrationBuilder.DropIndex(
                name: "IX_scheduler_item_id",
                table: "scheduler");

            migrationBuilder.DropColumn(
                name: "item_id",
                table: "scheduler");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .Annotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .Annotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .Annotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .Annotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .Annotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .Annotation("Npgsql:Enum:session_state", "ended,paused,running")
                .Annotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .Annotation("Npgsql:Enum:workflow", "deadline,scheduling,task")
                .OldAnnotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .OldAnnotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .OldAnnotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .OldAnnotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .OldAnnotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .OldAnnotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .OldAnnotation("Npgsql:Enum:session_state", "ended,paused,running")
                .OldAnnotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .OldAnnotation("Npgsql:Enum:workflow", "deadline,task");

            migrationBuilder.AddForeignKey(
                name: "FK_scheduler_items_id",
                table: "scheduler",
                column: "id",
                principalTable: "items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scheduler_items_id",
                table: "scheduler");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .Annotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .Annotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .Annotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .Annotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .Annotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .Annotation("Npgsql:Enum:session_state", "ended,paused,running")
                .Annotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .Annotation("Npgsql:Enum:workflow", "deadline,task")
                .OldAnnotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .OldAnnotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .OldAnnotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .OldAnnotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .OldAnnotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .OldAnnotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .OldAnnotation("Npgsql:Enum:session_state", "ended,paused,running")
                .OldAnnotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .OldAnnotation("Npgsql:Enum:workflow", "deadline,scheduling,task");

            migrationBuilder.AddColumn<Guid>(
                name: "item_id",
                table: "scheduler",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_scheduler_item_id",
                table: "scheduler",
                column: "item_id");

            migrationBuilder.AddForeignKey(
                name: "FK_scheduler_items_item_id",
                table: "scheduler",
                column: "item_id",
                principalTable: "items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
