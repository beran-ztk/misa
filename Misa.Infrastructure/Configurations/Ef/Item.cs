using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Misa.Infrastructure.Configurations.Ef;

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
            .HasColumnName("state_id");
        
        builder.Property(x => x.PriorityId)
            .IsRequired()
            .HasColumnName("priority_id");
        
        builder.Property(x => x.CategoryId)
            .IsRequired()
            .HasColumnName("category_id");
        
        builder.Property(x => x.Title)
            .IsRequired()
            .HasColumnName("title");
        
        // Entity 1:1
        builder.HasOne(i => i.Entity)
            .WithOne()
            .HasForeignKey<Domain.Items.Item>(i => i.EntityId)
            .OnDelete(DeleteBehavior.Cascade);
        
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
    }
}