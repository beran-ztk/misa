using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Zettelkasten;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Zettelkasten;

public sealed class ZettelConfiguration : IEntityTypeConfiguration<Zettel>
{
    public void Configure(EntityTypeBuilder<Zettel> builder)
    {
        builder.ToTable("zettel");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ItemId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Content);
    }
}
