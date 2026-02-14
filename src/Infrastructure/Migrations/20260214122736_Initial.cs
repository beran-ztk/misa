using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;
using Misa.Domain.Features.Messaging;

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
                .Annotation("Npgsql:Enum:workflow", "deadline,scheduling,task");

            migrationBuilder.CreateTable(
                name: "Entities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Workflow = table.Column<Workflow>(type: "workflow", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InteractedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<EventType>(type: "event_type", nullable: false),
                    EventState = table.Column<OutboxEventState>(type: "outbox_event_state", nullable: false, defaultValue: OutboxEventState.Pending),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    TimeZone = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeType = table.Column<ChangeType>(type: "change_type", nullable: false),
                    ValueBefore = table.Column<string>(type: "text", nullable: true),
                    ValueAfter = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditChanges_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Descriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Descriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Descriptions_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Priority = table.Column<Priority>(type: "priority", nullable: false, defaultValue: Priority.None),
                    Title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Entities_Id",
                        column: x => x.Id,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Deadline",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deadline", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_Deadline_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Schedulers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduleFrequencyType = table.Column<ScheduleFrequencyType>(type: "schedule_frequency_type", nullable: false, defaultValue: ScheduleFrequencyType.Once),
                    FrequencyInterval = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    OccurrenceCountLimit = table.Column<int>(type: "integer", nullable: true),
                    ByDay = table.Column<int[]>(type: "integer[]", nullable: true),
                    ByMonthDay = table.Column<int[]>(type: "integer[]", nullable: true),
                    ByMonth = table.Column<int[]>(type: "integer[]", nullable: true),
                    MisfirePolicy = table.Column<ScheduleMisfirePolicy>(type: "schedule_misfire_policy", nullable: false, defaultValue: ScheduleMisfirePolicy.Catchup),
                    LookaheadLimit = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    OccurrenceTtl = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ActionType = table.Column<ScheduleActionType>(type: "schedule_action_type", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    Timezone = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ActiveFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActiveUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastRunAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextDueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextAllowedExecutionAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedulers", x => x.Id);
                    table.CheckConstraint("ck_scheduler_active_date", "\"ActiveUntilUtc\" IS NULL OR \"ActiveUntilUtc\" > \"ActiveFromUtc\"");
                    table.CheckConstraint("ck_scheduler_active_time", "(\"StartTime\" IS NULL AND \"EndTime\" IS NULL) OR (\"StartTime\" IS NOT NULL AND \"EndTime\" IS NOT NULL AND \"StartTime\" < \"EndTime\")");
                    table.CheckConstraint("ck_scheduler_lookahead_limit_gt_0", "\"LookaheadLimit\" > 0");
                    table.CheckConstraint("ck_scheduler_next_due_ge_last_run", "\"NextDueAtUtc\" IS NULL OR \"LastRunAtUtc\" IS NULL OR \"NextDueAtUtc\" >= \"LastRunAtUtc\"");
                    table.CheckConstraint("ck_scheduler_occurrence_count_limit_ge_1", "\"OccurrenceCountLimit\" IS NULL OR \"OccurrenceCountLimit\" >= 0");
                    table.CheckConstraint("ck_scheduler_ttl_timespan", "\"OccurrenceTtl\" IS NULL OR \"OccurrenceTtl\" > INTERVAL '0'");
                    table.ForeignKey(
                        name: "FK_Schedulers_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<SessionState>(type: "session_state", nullable: false, defaultValue: SessionState.Running),
                    Efficiency = table.Column<SessionEfficiencyType>(type: "session_efficiency_type", nullable: false, defaultValue: SessionEfficiencyType.None),
                    Concentration = table.Column<SessionConcentrationType>(type: "session_concentration_type", nullable: false, defaultValue: SessionConcentrationType.None),
                    Objective = table.Column<string>(type: "text", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    AutoStopReason = table.Column<string>(type: "text", nullable: true),
                    PlannedDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    StopAutomatically = table.Column<bool>(type: "boolean", nullable: false),
                    WasAutomaticallyStopped = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<TaskCategory>(type: "task_category", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchedulerExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchedulerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledForUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClaimedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<SchedulerExecutionStatus>(type: "scheduler_execution_status", nullable: false, defaultValue: SchedulerExecutionStatus.Pending),
                    Error = table.Column<string>(type: "text", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulerExecutionLogs", x => x.Id);
                    table.CheckConstraint("ck_schedexec_after_claimed_requires_started", "\"Status\" IN ('pending','claimed') OR \"StartedAtUtc\" IS NOT NULL");
                    table.CheckConstraint("ck_schedexec_claimed_le_started_or_started_null", "\"StartedAtUtc\" IS NULL OR \"ClaimedAtUtc\" <= \"StartedAtUtc\"");
                    table.CheckConstraint("ck_schedexec_done_requires_finished", "\"Status\" NOT IN ('succeeded','failed','skipped') OR \"FinishedAtUtc\" IS NOT NULL");
                    table.CheckConstraint("ck_schedexec_not_pending_requires_claimed", "\"Status\" = 'pending' OR \"ClaimedAtUtc\" IS NOT NULL");
                    table.CheckConstraint("ck_schedexec_pending_has_no_timestamps", "\"Status\" <> 'pending' OR (\"ClaimedAtUtc\" IS NULL AND \"StartedAtUtc\" IS NULL AND \"FinishedAtUtc\" IS NULL)");
                    table.CheckConstraint("ck_schedexec_started_le_finished_or_finished_null", "\"FinishedAtUtc\" IS NULL OR \"StartedAtUtc\" <= \"FinishedAtUtc\"");
                    table.ForeignKey(
                        name: "FK_SchedulerExecutionLogs_Schedulers_SchedulerId",
                        column: x => x.SchedulerId,
                        principalTable: "Schedulers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PauseReason = table.Column<string>(type: "text", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionSegments_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditChanges_EntityId",
                table: "AuditChanges",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Descriptions_EntityId",
                table: "Descriptions",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerExecutionLogs_SchedulerId_ScheduledForUtc",
                table: "SchedulerExecutionLogs",
                columns: new[] { "SchedulerId", "ScheduledForUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ItemId",
                table: "Sessions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionSegments_SessionId",
                table: "SessionSegments",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditChanges");

            migrationBuilder.DropTable(
                name: "Deadline");

            migrationBuilder.DropTable(
                name: "Descriptions");

            migrationBuilder.DropTable(
                name: "Outbox");

            migrationBuilder.DropTable(
                name: "SchedulerExecutionLogs");

            migrationBuilder.DropTable(
                name: "SessionSegments");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Schedulers");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Entities");
        }
    }
}
