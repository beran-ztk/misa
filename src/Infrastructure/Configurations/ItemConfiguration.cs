using Misa.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ParentId);
        
        builder.Property(x => x.Kind);
        
        builder.Property(x => x.Title);
        
        builder.Property(x => x.IsExpanded);
        
        builder.HasOne(x => x.Note)
            .WithOne()
            .HasForeignKey<Note>(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.Quest)
            .WithOne()
            .HasForeignKey<Quest>(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}