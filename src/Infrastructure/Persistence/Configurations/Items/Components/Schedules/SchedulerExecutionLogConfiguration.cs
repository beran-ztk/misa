using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Schedules;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Schedules;

public sealed class SchedulerExecutionLogConfiguration : IEntityTypeConfiguration<ScheduleExecutionLog>
{
    public void Configure(EntityTypeBuilder<ScheduleExecutionLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.SchedulerId)
            .HasConversion(s => s.Value, value => new ItemId(value));

        builder.Property(e => e.ScheduledForUtc);

        builder.Property(e => e.ClaimedAtUtc);

        builder.Property(e => e.StartedAtUtc);

        builder.Property(e => e.FinishedAtUtc);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasDefaultValue(ScheduleExecutionStatus.Pending);

        builder.Property(e => e.Error);

        builder.Property(e => e.Attempts)
            .HasDefaultValue(0);

        builder.Property(e => e.CreatedAtUtc);
        
        // Constraints
        builder.HasIndex(e => new { e.SchedulerId, e.ScheduledForUtc });
        
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
    }
}
