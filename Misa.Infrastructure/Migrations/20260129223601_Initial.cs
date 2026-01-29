using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                .Annotation("Npgsql:Enum:workflow", "deadline,scheduling,task");

            migrationBuilder.CreateTable(
                name: "entities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workflow = table.Column<Workflow>(type: "workflow", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    archived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    interacted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "item_states",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field = table.Column<ChangeType>(type: "change_type", nullable: false),
                    value_before = table.Column<string>(type: "text", nullable: true),
                    value_after = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_changes", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_changes_entities_entity_id",
                        column: x => x.entity_id,
                        principalTable: "entities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "descriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_descriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_descriptions_entities_entity_id",
                        column: x => x.entity_id,
                        principalTable: "entities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    priority = table.Column<Priority>(type: "priority", nullable: false, defaultValue: Priority.None),
                    title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_items_entities_id",
                        column: x => x.id,
                        principalTable: "entities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_items_item_states_state_id",
                        column: x => x.state_id,
                        principalTable: "item_states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_deadlines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deadline_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "scheduler",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    frequency_type = table.Column<ScheduleFrequencyType>(type: "schedule_frequency_type", nullable: false, defaultValue: ScheduleFrequencyType.Once),
                    frequency_interval = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    occurrence_count_limit = table.Column<int>(type: "integer", nullable: true),
                    by_day = table.Column<int[]>(type: "integer[]", nullable: true),
                    by_month_day = table.Column<int[]>(type: "integer[]", nullable: true),
                    by_month = table.Column<int[]>(type: "integer[]", nullable: true),
                    misfire_policy = table.Column<ScheduleMisfirePolicy>(type: "schedule_misfire_policy", nullable: false, defaultValue: ScheduleMisfirePolicy.Catchup),
                    lookahead_limit = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    occurrence_ttl = table.Column<TimeSpan>(type: "interval", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    timezone = table.Column<string>(type: "text", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    active_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    active_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_run_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_allowed_execution_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduler", x => x.id);
                    table.CheckConstraint("ck_scheduler_active_date", "active_until_utc IS NULL OR active_until_utc > active_from_utc");
                    table.CheckConstraint("ck_scheduler_active_time", "(start_time IS NULL AND end_time IS NULL) OR (start_time IS NOT NULL AND end_time IS NOT NULL AND start_time < end_time)");
                    table.CheckConstraint("ck_scheduler_lookahead_limit_gt_0", "lookahead_limit > 0");
                    table.CheckConstraint("ck_scheduler_next_due_ge_last_run", "next_due_at_utc IS NULL OR last_run_at_utc IS NULL OR next_due_at_utc >= last_run_at_utc");
                    table.CheckConstraint("ck_scheduler_occurrence_count_limit_ge_1", "occurrence_count_limit IS NULL OR occurrence_count_limit >= 1");
                    table.CheckConstraint("ck_scheduler_ttl_timespan", "occurrence_ttl IS NULL OR occurrence_ttl > INTERVAL '0'");
                    table.ForeignKey(
                        name: "FK_scheduler_items_id",
                        column: x => x.id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<SessionState>(type: "session_state", nullable: false, defaultValue: SessionState.Running),
                    efficiency = table.Column<SessionEfficiencyType>(type: "session_efficiency_type", nullable: false, defaultValue: SessionEfficiencyType.None),
                    concentration = table.Column<SessionConcentrationType>(type: "session_concentration_type", nullable: false, defaultValue: SessionConcentrationType.None),
                    objective = table.Column<string>(type: "text", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    auto_stop_reason = table.Column<string>(type: "text", nullable: true),
                    planned_duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    stop_automatically = table.Column<bool>(type: "boolean", nullable: false),
                    was_automatically_stopped = table.Column<bool>(type: "boolean", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.id);
                    table.CheckConstraint("ck_planned_duration", "planned_duration IS NULL OR (planned_duration > INTERVAL '0 seconds' AND planned_duration < INTERVAL '1 day')");
                    table.ForeignKey(
                        name: "FK_sessions_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "scheduler_execution_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    scheduler_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_for_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    claimed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<SchedulerExecutionStatus>(type: "schedule_execution_state", nullable: false, defaultValue: SchedulerExecutionStatus.Pending),
                    error = table.Column<string>(type: "text", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduler_execution_log", x => x.id);
                    table.CheckConstraint("ck_schedexec_after_claimed_requires_started", "status IN ('pending','claimed') OR started_at_utc IS NOT NULL");
                    table.CheckConstraint("ck_schedexec_claimed_le_started_or_started_null", "started_at_utc IS NULL OR claimed_at_utc <= started_at_utc");
                    table.CheckConstraint("ck_schedexec_done_requires_finished", "status NOT IN ('succeeded','failed','skipped') OR finished_at_utc IS NOT NULL");
                    table.CheckConstraint("ck_schedexec_not_pending_requires_claimed", "status = 'pending' OR claimed_at_utc IS NOT NULL");
                    table.CheckConstraint("ck_schedexec_pending_has_no_timestamps", "status <> 'pending' OR (claimed_at_utc IS NULL AND started_at_utc IS NULL AND finished_at_utc IS NULL)");
                    table.CheckConstraint("ck_schedexec_started_le_finished_or_finished_null", "finished_at_utc IS NULL OR started_at_utc <= finished_at_utc");
                    table.ForeignKey(
                        name: "FK_scheduler_execution_log_scheduler_scheduler_id",
                        column: x => x.scheduler_id,
                        principalTable: "scheduler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "session_segments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pause_reason = table.Column<string>(type: "text", nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_segments", x => x.id);
                    table.CheckConstraint("ck_session_segments_ended_after_started", "ended_at_utc IS NULL OR started_at_utc <= ended_at_utc");
                    table.ForeignKey(
                        name: "FK_session_segments_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "item_states",
                columns: new[] { "id", "name", "synopsis" },
                values: new object[,]
                {
                    { 1, "Draft", "Entwurf; noch nie daran gearbeitet" },
                    { 2, "Undefined", "Unklar; muss präzisiert werden" },
                    { 3, "Scheduled", "Geplant für einen zukünftigen Zeitpunkt" },
                    { 4, "InProgress", "Bereits bearbeitet, aktuell keine aktive Session" },
                    { 5, "Active", "Aktive Session läuft" },
                    { 6, "Paused", "Session pausiert (max. 6h, danach Auto-Fortsetzung)" },
                    { 7, "Pending", "Zurückgestellt; lange nicht bearbeitet" },
                    { 8, "WaitForResponse", "Wartet auf Rückmeldung einer Person oder Stelle" },
                    { 9, "BlockedByRelationship", "Blockiert durch Relation oder Abhängigkeit" },
                    { 10, "Done", "Erfolgreich abgeschlossen" },
                    { 11, "Canceled", "Abgebrochen; nicht weiter erforderlich" },
                    { 12, "Failed", "Gescheitert; Ziel nicht erreicht" },
                    { 13, "Expired", "Automatisch abgelaufen (Deadline überschritten)" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_changes_entity_id",
                table: "audit_changes",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "IX_descriptions_entity_id",
                table: "descriptions",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_state_id",
                table: "items",
                column: "state_id");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_deadlines_item_id",
                table: "scheduled_deadlines",
                column: "item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scheduler_execution_log_scheduler_id_scheduled_for_utc",
                table: "scheduler_execution_log",
                columns: new[] { "scheduler_id", "scheduled_for_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_segments_session_id",
                table: "session_segments",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_item_id",
                table: "sessions",
                column: "item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_changes");

            migrationBuilder.DropTable(
                name: "descriptions");

            migrationBuilder.DropTable(
                name: "scheduled_deadlines");

            migrationBuilder.DropTable(
                name: "scheduler_execution_log");

            migrationBuilder.DropTable(
                name: "session_segments");

            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.DropTable(
                name: "scheduler");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "entities");

            migrationBuilder.DropTable(
                name: "item_states");
        }
    }
}
