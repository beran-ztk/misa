using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:priority", "critical,high,low,medium,none,urgent")
                .Annotation("Npgsql:Enum:schedule_frequency_type", "days,hours,minutes,months,once,weeks,years")
                .Annotation("Npgsql:Enum:schedule_misfire_policy", "catchup,run_once,skip");

            migrationBuilder.CreateTable(
                name: "action_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_action_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entity_workflow_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_workflow_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "item_states",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "session_concentration_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_concentration_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "session_efficiency_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_efficiency_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "session_states",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workflow_id = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    archived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    interacted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entities", x => x.id);
                    table.ForeignKey(
                        name: "FK_entities_entity_workflow_types_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "entity_workflow_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workflow_category_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workflow_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_category_types", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_category_types_entity_workflow_types_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "entity_workflow_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_id = table.Column<int>(type: "integer", nullable: false),
                    value_before = table.Column<string>(type: "text", nullable: true),
                    value_after = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_actions", x => x.id);
                    table.ForeignKey(
                        name: "FK_actions_action_types_type_id",
                        column: x => x.type_id,
                        principalTable: "action_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_actions_entities_entity_id",
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
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    priority = table.Column<Priority>(type: "priority", nullable: false, defaultValue: Priority.None),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.entity_id);
                    table.ForeignKey(
                        name: "FK_items_entities_entity_id",
                        column: x => x.entity_id,
                        principalTable: "entities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_items_item_states_state_id",
                        column: x => x.state_id,
                        principalTable: "item_states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_workflow_category_types_category_id",
                        column: x => x.category_id,
                        principalTable: "workflow_category_types",
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
                        principalColumn: "entity_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scheduler",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    frequency_type = table.Column<ScheduleFrequencyType>(type: "schedule_frequency_type", nullable: false, defaultValue: ScheduleFrequencyType.Once),
                    frequency_interval = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    occurrence_count_limit = table.Column<int>(type: "integer", nullable: true),
                    by_day = table.Column<int[]>(type: "integer[]", nullable: true),
                    by_month_day = table.Column<int[]>(type: "integer[]", nullable: true),
                    by_month = table.Column<int[]>(type: "integer[]", nullable: true),
                    misfire_policy = table.Column<ScheduleMisfirePolicy>(type: "schedule_misfire_policy", nullable: false, defaultValue: ScheduleMisfirePolicy.Catchup),
                    lookahead_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    occurrence_ttl = table.Column<TimeSpan>(type: "interval", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    timezone = table.Column<string>(type: "text", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    active_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    active_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_run_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduler", x => x.id);
                    table.ForeignKey(
                        name: "FK_scheduler_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "entity_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_id = table.Column<int>(type: "integer", nullable: false),
                    efficiency_id = table.Column<int>(type: "integer", nullable: true),
                    concentration_id = table.Column<int>(type: "integer", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_sessions_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "entity_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sessions_session_concentration_types_concentration_id",
                        column: x => x.concentration_id,
                        principalTable: "session_concentration_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sessions_session_efficiency_types_efficiency_id",
                        column: x => x.efficiency_id,
                        principalTable: "session_efficiency_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sessions_session_states_state_id",
                        column: x => x.state_id,
                        principalTable: "session_states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    error = table.Column<string>(type: "text", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduler_execution_log", x => x.id);
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
                    table.ForeignKey(
                        name: "FK_session_segments_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_action_types_name",
                table: "action_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_actions_entity_id",
                table: "actions",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "IX_actions_type_id",
                table: "actions",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "IX_descriptions_entity_id",
                table: "descriptions",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "IX_entities_workflow_id",
                table: "entities",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_category_id",
                table: "items",
                column: "category_id");

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
                name: "IX_scheduler_item_id",
                table: "scheduler",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_scheduler_execution_log_scheduler_id",
                table: "scheduler_execution_log",
                column: "scheduler_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_concentration_types_name",
                table: "session_concentration_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_concentration_types_sort_order",
                table: "session_concentration_types",
                column: "sort_order",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_efficiency_types_name",
                table: "session_efficiency_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_efficiency_types_sort_order",
                table: "session_efficiency_types",
                column: "sort_order",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_segments_session_id",
                table: "session_segments",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_states_name",
                table: "session_states",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sessions_concentration_id",
                table: "sessions",
                column: "concentration_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_efficiency_id",
                table: "sessions",
                column: "efficiency_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_item_id",
                table: "sessions",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_state_id",
                table: "sessions",
                column: "state_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_category_types_workflow_id",
                table: "workflow_category_types",
                column: "workflow_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "actions");

            migrationBuilder.DropTable(
                name: "descriptions");

            migrationBuilder.DropTable(
                name: "scheduled_deadlines");

            migrationBuilder.DropTable(
                name: "scheduler_execution_log");

            migrationBuilder.DropTable(
                name: "session_segments");

            migrationBuilder.DropTable(
                name: "action_types");

            migrationBuilder.DropTable(
                name: "scheduler");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "session_concentration_types");

            migrationBuilder.DropTable(
                name: "session_efficiency_types");

            migrationBuilder.DropTable(
                name: "session_states");

            migrationBuilder.DropTable(
                name: "entities");

            migrationBuilder.DropTable(
                name: "item_states");

            migrationBuilder.DropTable(
                name: "workflow_category_types");

            migrationBuilder.DropTable(
                name: "entity_workflow_types");
        }
    }
}
