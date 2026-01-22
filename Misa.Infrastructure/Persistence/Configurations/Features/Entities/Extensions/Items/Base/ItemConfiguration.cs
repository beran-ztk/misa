using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Deadlines;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Base;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");
        
        builder.HasKey(x => x.EntityId);
        
        
        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id");
        
        builder.Property(x => x.StateId)
            .IsRequired()
            .HasColumnName("state_id")
            .HasDefaultValue((int)ItemStates.Draft);
        
        builder.Property(x => x.Priority)
            .IsRequired()
            .HasColumnName("priority")
            .HasColumnType("priority")
            .HasDefaultValue(Priority.None);
        
        builder.Property(x => x.Title)
            .IsRequired()
            .HasColumnName("title");
        
        // State n:1
        builder.HasOne(i => i.State)
            .WithMany()
            .HasForeignKey(i => i.StateId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Deadline 1:0..1
        builder.HasOne(i => i.ScheduledDeadline)
            .WithOne()
            .HasForeignKey<ScheduledDeadline>(d => d.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Session 1:0..1
        builder.HasMany(e => e.Sessions)
            .WithOne()
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}