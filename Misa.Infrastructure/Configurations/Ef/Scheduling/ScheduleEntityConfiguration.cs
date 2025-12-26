using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Configurations.Ef.Scheduling;

public class ScheduleEntityConfiguration : IEntityTypeConfiguration<Domain.Scheduling.Schedule>
{
    public void Configure(EntityTypeBuilder<Domain.Scheduling.Schedule> builder)
    {
        builder.ToTable("schedule");

        builder.HasKey(s => s.EntityId);

        builder.Property(s => s.EntityId)
            .HasColumnName("entity_id");
        
        builder.Property(s => s.StartAtUtc)
            .IsRequired()
            .HasColumnName("start_at_utc");
        
        builder.Property(s => s.EndAtUtc)
            .IsRequired(false)
            .HasColumnName("end_at_utc");

        builder.HasOne<Domain.Entities.Entity>()
            .WithOne()
            .HasForeignKey<Domain.Scheduling.Schedule>(s => s.EntityId);
    }
}