using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Deadlines;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Features.Deadlines;

public sealed class DeadlineConfiguration : IEntityTypeConfiguration<ScheduledDeadline>
{
    public void Configure(EntityTypeBuilder<ScheduledDeadline> builder)
    {
        builder.ToTable("scheduled_deadlines");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.ItemId)
            .IsRequired()
            .HasColumnName("item_id");

        builder.Property(d => d.DeadlineAtUtc)
            .IsRequired()
            .HasColumnName("deadline_at_utc");

        // 0..1 Deadline pro Item
        builder.HasIndex(d => d.ItemId)
            .IsUnique();

        builder.HasOne<Domain.Features.Entities.Extensions.Items.Base.Item>()
            .WithOne(i => i.ScheduledDeadline)
            .HasForeignKey<ScheduledDeadline>(d => d.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}