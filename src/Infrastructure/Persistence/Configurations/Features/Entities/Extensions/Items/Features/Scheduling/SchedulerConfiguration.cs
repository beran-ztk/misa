using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Features.Scheduling;

public sealed class SchedulerConfiguration : IEntityTypeConfiguration<Scheduler>
{
    public void Configure(EntityTypeBuilder<Scheduler> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(s => s.TargetItemId);
        
        builder.Property(s => s.ScheduleFrequencyType)
            .IsRequired()
            .HasDefaultValue(ScheduleFrequencyType.Once);

        builder.Property(s => s.FrequencyInterval)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(s => s.OccurrenceCountLimit);

        builder.Property(s => s.ByDay);

        builder.Property(s => s.ByMonthDay);

        builder.Property(s => s.ByMonth);

        builder.Property(s => s.MisfirePolicy)
            .IsRequired()
            .HasDefaultValue(ScheduleMisfirePolicy.Catchup);

        builder.Property(s => s.LookaheadLimit)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(s => s.OccurrenceTtl);

        builder.Property(s => s.ActionType)
            .IsRequired();
        
        builder.Property(s => s.Payload)
            .HasColumnType("jsonb");

        builder.Property(s => s.Timezone)
            .IsRequired();

        builder.Property(s => s.StartTime);

        builder.Property(s => s.EndTime);

        builder.Property(s => s.ActiveFromUtc)
            .IsRequired();

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
            "\"OccurrenceCountLimit\" IS NULL OR \"OccurrenceCountLimit\" >= 1"
        );

        builder.HasCheckConstraint(
            "ck_scheduler_next_due_ge_last_run",
            "\"NextDueAtUtc\" IS NULL OR \"LastRunAtUtc\" IS NULL OR \"NextDueAtUtc\" >= \"LastRunAtUtc\""
        );
        
        // Relationships
        builder.HasOne(s => s.Item)
            .WithOne()
            .HasForeignKey<Scheduler>(s => s.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.ExecutionLogs)
            .WithOne(e => e.Scheduler)
            .HasForeignKey(e => e.SchedulerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
