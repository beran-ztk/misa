using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Activities;

public class ItemActivityConfiguration : IEntityTypeConfiguration<ItemActivity>
{
    public void Configure(EntityTypeBuilder<ItemActivity> builder)
    {
        builder.ToTable("item_activities");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new ItemId(value))
            .ValueGeneratedNever();

        builder.Property(a => a.State);

        builder.Property(a => a.Priority);

        builder.Property(a => a.DueAt);
        
        // Relations
        builder.HasMany(a => a.Sessions)
            .WithOne()
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}