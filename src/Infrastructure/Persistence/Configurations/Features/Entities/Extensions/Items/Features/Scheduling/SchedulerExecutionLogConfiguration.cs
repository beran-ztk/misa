using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Features.Scheduling;

public sealed class SchedulerExecutionLogConfiguration : IEntityTypeConfiguration<SchedulerExecutionLog>
{
    public void Configure(EntityTypeBuilder<SchedulerExecutionLog> builder)
    {
        builder.ToTable("scheduler_execution_log", t =>
        {
            t.HasCheckConstraint(
                "ck_schedexec_claimed_le_started_or_started_null",
                "started_at_utc IS NULL OR claimed_at_utc <= started_at_utc"
            );
            t.HasCheckConstraint(
                "ck_schedexec_started_le_finished_or_finished_null",
                "finished_at_utc IS NULL OR started_at_utc <= finished_at_utc"
            );
            t.HasCheckConstraint(
                "ck_schedexec_pending_has_no_timestamps",
                "status <> 'pending' OR (claimed_at_utc IS NULL AND started_at_utc IS NULL AND finished_at_utc IS NULL)"
            );
            t.HasCheckConstraint(
                "ck_schedexec_not_pending_requires_claimed",
                "status = 'pending' OR claimed_at_utc IS NOT NULL"
            );
            t.HasCheckConstraint(
                "ck_schedexec_after_claimed_requires_started",
                "status IN ('pending','claimed') OR started_at_utc IS NOT NULL"
            );
            t.HasCheckConstraint(
                "ck_schedexec_done_requires_finished",
                "status NOT IN ('succeeded','failed','skipped') OR finished_at_utc IS NOT NULL"
            );
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.SchedulerId)
            .IsRequired()
            .HasColumnName("scheduler_id");

        builder.Property(e => e.ScheduledForUtc)
            .IsRequired()
            .HasColumnName("scheduled_for_utc");

        builder.Property(e => e.ClaimedAtUtc)
            .HasColumnName("claimed_at_utc");

        builder.Property(e => e.StartedAtUtc)
            .HasColumnName("started_at_utc");

        builder.Property(e => e.FinishedAtUtc)
            .HasColumnName("finished_at_utc");

        builder.Property(e => e.Status)
            .IsRequired()
            .HasColumnName("status")
            .HasColumnType("schedule_execution_state")
            .HasDefaultValue(SchedulerExecutionStatus.Pending);

        builder.Property(e => e.Error)
            .HasColumnName("error");

        builder.Property(e => e.Attempts)
            .IsRequired()
            .HasColumnName("attempts")
            .HasDefaultValue(0);

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()");
        
        // Constraints
        builder.HasIndex(e => new { e.SchedulerId, e.ScheduledForUtc })
            .IsUnique();
            
        // Relationship
        builder.HasOne(e => e.Scheduler)
            .WithMany(s => s.ExecutionLogs)
            .HasForeignKey(e => e.SchedulerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
