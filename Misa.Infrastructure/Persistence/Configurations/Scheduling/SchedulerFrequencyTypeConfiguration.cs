using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Scheduling;

namespace Misa.Infrastructure.Persistence.Configurations.Scheduling;

public sealed class SchedulerFrequencyTypeConfiguration : IEntityTypeConfiguration<SchedulerFrequencyType>
{
    public void Configure(EntityTypeBuilder<SchedulerFrequencyType> builder)
    {
        builder.ToTable("scheduler_frequency_types");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(f => f.Name)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(f => f.Synopsis)
            .HasColumnName("synopsis");
    }
}
