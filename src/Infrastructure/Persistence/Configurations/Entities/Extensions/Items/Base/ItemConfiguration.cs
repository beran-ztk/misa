using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Extensions.Items.Base;

namespace Misa.Infrastructure.Persistence.Configurations.Entities.Extensions.Items.Base;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id);
        
        builder.Property(x => x.StateId)
            .IsRequired()
            .HasDefaultValue((int)ItemStates.Draft);
        
        builder.Property(x => x.Priority)
            .IsRequired()
            .HasDefaultValue(Priority.None);
        
        builder.Property(x => x.Title)
            .IsRequired();
        
        // Relations
        builder.HasOne(i => i.Entity)
            .WithOne()
            .HasForeignKey<Item>(i => i.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.State)
            .WithMany()
            .HasForeignKey(i => i.StateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Sessions)
            .WithOne()
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}