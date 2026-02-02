using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Features.Scheduling;

public sealed class SchedulerExecutionLogConfiguration : IEntityTypeConfiguration<SchedulerExecutionLog>
{
    public void Configure(EntityTypeBuilder<SchedulerExecutionLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.SchedulerId)
            .IsRequired();

        builder.Property(e => e.ScheduledForUtc)
            .IsRequired();

        builder.Property(e => e.ClaimedAtUtc);

        builder.Property(e => e.StartedAtUtc);

        builder.Property(e => e.FinishedAtUtc);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasDefaultValue(SchedulerExecutionStatus.Pending);

        builder.Property(e => e.Error);

        builder.Property(e => e.Attempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("now()");
        
        // Constraints
        builder.HasIndex(e => new { e.SchedulerId, e.ScheduledForUtc })
            .IsUnique();
        
        builder.HasCheckConstraint(
            "ck_schedexec_claimed_le_started_or_started_null",
            "\"StartedAtUtc\" IS NULL OR \"ClaimedAtUtc\" <= \"StartedAtUtc\""
        );

        builder.HasCheckConstraint(
            "ck_schedexec_started_le_finished_or_finished_null",
            "\"FinishedAtUtc\" IS NULL OR \"StartedAtUtc\" <= \"FinishedAtUtc\""
        );

        builder.HasCheckConstraint(
            "ck_schedexec_pending_has_no_timestamps",
            "\"Status\" <> 'pending' OR (\"ClaimedAtUtc\" IS NULL AND \"StartedAtUtc\" IS NULL AND \"FinishedAtUtc\" IS NULL)"
        );

        builder.HasCheckConstraint(
            "ck_schedexec_not_pending_requires_claimed",
            "\"Status\" = 'pending' OR \"ClaimedAtUtc\" IS NOT NULL"
        );

        builder.HasCheckConstraint(
            "ck_schedexec_after_claimed_requires_started",
            "\"Status\" IN ('pending','claimed') OR \"StartedAtUtc\" IS NOT NULL"
        );

        builder.HasCheckConstraint(
            "ck_schedexec_done_requires_finished",
            "\"Status\" NOT IN ('succeeded','failed','skipped') OR \"FinishedAtUtc\" IS NOT NULL"
        );
        
        // Relationship
        builder.HasOne(e => e.Scheduler)
            .WithMany(s => s.ExecutionLogs)
            .HasForeignKey(e => e.SchedulerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
