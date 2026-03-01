using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Schola;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Scholar;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("item_units");
        
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ItemId(value))
            .ValueGeneratedNever();
        
        var arcIdConverter = new ValueConverter<ItemId?, Guid?>(
            id => id == null ? null : id.Value.Value,
            value => value == null ? null : new ItemId(value.Value)
        );

        builder.Property(x => x.ArcId)
            .HasConversion(arcIdConverter);
    }
}