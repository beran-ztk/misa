using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Deadlines;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Base;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id");
        
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
        
        // Relations
        builder.HasOne(i => i.Entity)
            .WithOne()
            .HasForeignKey<Item>(i => i.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.State)
            .WithMany()
            .HasForeignKey(i => i.StateId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(i => i.ScheduledDeadline)
            .WithOne()
            .HasForeignKey<ScheduledDeadline>(d => d.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Sessions)
            .WithOne()
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}