using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Scheduling;

namespace Misa.Infrastructure.Persistence.Configurations.Scheduling;

public sealed class SchedulerExecutionLogConfiguration : IEntityTypeConfiguration<SchedulerExecutionLog>
{
    public void Configure(EntityTypeBuilder<SchedulerExecutionLog> builder)
    {
        builder.ToTable("scheduler_execution_log");

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
            .HasConversion<string>()
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

        // Relationship
        builder.HasOne(e => e.Scheduler)
            .WithMany(s => s.ExecutionLogs)
            .HasForeignKey(e => e.SchedulerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
