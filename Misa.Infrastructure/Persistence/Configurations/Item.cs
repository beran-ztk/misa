using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Persistence.Configurations;

public class Item : IEntityTypeConfiguration<Misa.Domain.Items.Item>
{
    public void Configure(EntityTypeBuilder<Misa.Domain.Items.Item> builder)
    {
        builder.ToTable("items");
        
        builder.HasKey(x => x.EntityId);
        
        
        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id");
        
        builder.Property(x => x.StateId)
            .IsRequired()
            .HasColumnName("state_id")
            .HasDefaultValue((int)Misa.Domain.Dictionaries.Items.ItemStates.Draft);
        
        builder.Property(x => x.PriorityId)
            .IsRequired()
            .HasColumnName("priority_id");
        
        builder.Property(x => x.CategoryId)
            .IsRequired()
            .HasColumnName("category_id");
        
        builder.Property(x => x.Title)
            .IsRequired()
            .HasColumnName("title");
        
        // State n:1
        builder.HasOne(i => i.State)
            .WithMany()
            .HasForeignKey(i => i.StateId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Priority n:1
        builder.HasOne(i => i.Priority)
            .WithMany()
            .HasForeignKey(i => i.PriorityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category n:1
        builder.HasOne(i => i.Category)
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Deadline 1:0..1
        builder.HasOne(i => i.ScheduledDeadline)
            .WithOne()
            .HasForeignKey<Misa.Domain.Scheduling.ScheduledDeadline>(d => d.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Session 1:0..1
        builder.HasMany(e => e.Sessions)
            .WithOne()
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}