using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Schedules;
using Wolverine;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Schedules;

public sealed class ScheduleConfiguration : IEntityTypeConfiguration<ScheduleExtension>
{
    public void Configure(EntityTypeBuilder<ScheduleExtension> builder)
    {
        builder.ToTable("item_schedules");
        
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(s => s.Value, value => new ItemId(value))
            .ValueGeneratedNever();
        
        builder.Property(s => s.TargetItemId);
        
        builder.Property(s => s.ScheduleFrequencyType)
            .HasDefaultValue(ScheduleFrequencyType.Once);

        builder.Property(s => s.FrequencyInterval)
            .HasDefaultValue(1);

        builder.Property(s => s.OccurrenceCountLimit);

        builder.Property(s => s.ByDay);

        builder.Property(s => s.ByMonthDay);

        builder.Property(s => s.ByMonth);

        builder.Property(s => s.MisfirePolicy)
            .HasDefaultValue(ScheduleMisfirePolicy.Catchup);

        builder.Property(s => s.LookaheadLimit)
            .HasDefaultValue(1);

        builder.Property(s => s.OccurrenceTtl);

        builder.Property(s => s.ActionType);
        
        builder.Property(s => s.Payload)
            .HasColumnType("jsonb");

        builder.Property(s => s.Timezone);

        builder.Property(s => s.StartTime);

        builder.Property(s => s.EndTime);

        builder.Property(s => s.ActiveFromUtc);

        builder.Property(s => s.ActiveUntilUtc);

        builder.Property(s => s.LastRunAtUtc);

        builder.Property(s => s.NextDueAtUtc);

        builder.Property(s => s.NextAllowedExecutionAtUtc);
        
        // Constraints
        builder.HasCheckConstraint(
            "ck_scheduler_lookahead_limit_gt_0",
            "\"LookaheadLimit\" > 0"
        );

        builder.HasCheckConstraint(
            "ck_scheduler_ttl_timespan",
            "\"OccurrenceTtl\" IS NULL OR \"OccurrenceTtl\" > INTERVAL '0'"
        );

        builder.HasCheckConstraint(
            "ck_scheduler_active_time",
            "(\"StartTime\" IS NULL AND \"EndTime\" IS NULL) " +
            "OR (\"StartTime\" IS NOT NULL AND \"EndTime\" IS NOT NULL AND \"StartTime\" < \"EndTime\")"
        );

        builder.HasCheckConstraint(
            "ck_scheduler_active_date",
            "\"ActiveUntilUtc\" IS NULL OR \"ActiveUntilUtc\" > \"ActiveFromUtc\""
        );

        builder.HasCheckConstraint(
            "ck_scheduler_occurrence_count_limit_ge_1",
            "\"OccurrenceCountLimit\" IS NULL OR \"OccurrenceCountLimit\" >= 0"
        );

        builder.HasCheckConstraint(
            "ck_scheduler_next_due_ge_last_run",
            "\"NextDueAtUtc\" IS NULL OR \"LastRunAtUtc\" IS NULL OR \"NextDueAtUtc\" >= \"LastRunAtUtc\""
        );
        
        // Relationships
        builder.HasMany(s => s.ExecutionLogs)
            .WithOne()
            .HasForeignKey(e => e.SchedulerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
