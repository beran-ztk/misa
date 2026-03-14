using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Zettelkasten;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Zettelkasten;

public sealed class ZettelExtensionConfiguration : IEntityTypeConfiguration<ZettelExtension>
{
    public void Configure(EntityTypeBuilder<ZettelExtension> builder)
    {
        builder.ToTable("zettels");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ItemId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TopicId)
            .HasConversion(
                new ValueConverter<ItemId, Guid>(id => id.Value, value => new ItemId(value)));

        builder.Property(x => x.Content);
    }
}
