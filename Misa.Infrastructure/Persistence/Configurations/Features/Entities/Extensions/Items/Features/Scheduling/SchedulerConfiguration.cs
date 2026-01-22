using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Features.Scheduling;

public sealed class SchedulerConfiguration : IEntityTypeConfiguration<Scheduler>
{
    public void Configure(EntityTypeBuilder<Scheduler> builder)
    {
        builder.ToTable("scheduler");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.ItemId)
            .IsRequired()
            .HasColumnName("item_id");

        builder.Property(s => s.ScheduleFrequencyType)
            .IsRequired()
            .HasColumnName("frequency_type_id")
            .HasColumnType("scheduler_frequency_type")
            .HasDefaultValue(ScheduleFrequencyType.Once);

        builder.Property(s => s.FrequencyInterval)
            .IsRequired()
            .HasColumnName("frequency_interval")
            .HasDefaultValue(1);

        builder.Property(s => s.OccurrenceCountLimit)
            .HasColumnName("occurrence_count_limit");

        builder.Property(s => s.ByDay)
            .HasColumnName("by_day");

        builder.Property(s => s.ByMonthDay)
            .HasColumnName("by_month_day");

        builder.Property(s => s.ByMonth)
            .HasColumnName("by_month");

        builder.Property(s => s.MisfirePolicy)
            .IsRequired()
            .HasColumnName("misfire_policy")
            .HasColumnType("scheduler_misfire_policy")
            .HasDefaultValue(ScheduleMisfirePolicy.Catchup);

        builder.Property(s => s.LookaheadCount)
            .IsRequired()
            .HasColumnName("lookahead_count")
            .HasDefaultValue(1);

        builder.Property(s => s.OccurrenceTtl)
            .HasColumnName("occurrence_ttl");

        builder.Property(s => s.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb");

        builder.Property(s => s.Timezone)
            .IsRequired()
            .HasColumnName("timezone");

        builder.Property(s => s.StartTime)
            .HasColumnName("start_time");

        builder.Property(s => s.EndTime)
            .HasColumnName("end_time");

        builder.Property(s => s.ActiveFromUtc)
            .IsRequired()
            .HasColumnName("active_from_utc");

        builder.Property(s => s.ActiveUntilUtc)
            .HasColumnName("active_until_utc");

        builder.Property(s => s.LastRunAtUtc)
            .HasColumnName("last_run_at_utc");

        builder.Property(s => s.NextDueAtUtc)
            .HasColumnName("next_due_at_utc");

        // Relationships
        builder.HasOne<Domain.Features.Entities.Extensions.Items.Base.Item>()
            .WithMany()
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.ExecutionLogs)
            .WithOne(e => e.Scheduler)
            .HasForeignKey(e => e.SchedulerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
