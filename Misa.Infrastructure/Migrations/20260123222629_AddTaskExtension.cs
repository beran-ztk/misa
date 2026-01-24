using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks;

#nullable disable

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                .OldAnnotation("Npgsql:Enum:workflow", "deadline,task");

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<TaskCategory>(type: "task_category", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_tasks_items_id",
                        column: x => x.id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .Annotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .Annotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .Annotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .Annotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .Annotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .Annotation("Npgsql:Enum:session_state", "ended,paused,running")
                .Annotation("Npgsql:Enum:workflow", "deadline,task")
                .OldAnnotation("Npgsql:Enum:change_type", "category,deadline,priority,state,title")
                .OldAnnotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .OldAnnotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .OldAnnotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip")
                .OldAnnotation("Npgsql:Enum:session_concentration_type", "deep_focus,distracted,focused,hyperfocus,none,unfocused_but_calm")
                .OldAnnotation("Npgsql:Enum:session_efficiency_type", "high_output,low_output,none,peak_performance,steady_output")
                .OldAnnotation("Npgsql:Enum:session_state", "ended,paused,running")
                .OldAnnotation("Npgsql:Enum:task_category", "other,personal,school,work")
                .OldAnnotation("Npgsql:Enum:workflow", "deadline,task");
        }
    }
}
