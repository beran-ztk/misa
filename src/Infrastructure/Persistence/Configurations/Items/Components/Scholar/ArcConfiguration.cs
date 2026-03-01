using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Schola;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Scholar;

public sealed class ArcConfiguration : IEntityTypeConfiguration<Arc>
{
    public void Configure(EntityTypeBuilder<Arc> builder)
    {
        builder.ToTable("item_arcs");
        
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ItemId(value))
            .ValueGeneratedNever();
        
        builder.HasMany(a => a.Units)
            .WithOne()
            .HasForeignKey(s => s.ArcId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}