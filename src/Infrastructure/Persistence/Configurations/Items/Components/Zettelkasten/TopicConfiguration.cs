using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Zettelkasten;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Zettelkasten;

public sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("topics");
        
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ItemId(value))
            .ValueGeneratedNever();
        
        var arcIdConverter = new ValueConverter<ItemId?, Guid?>(
            id => id == null ? null : id.Value.Value,
            value => value == null ? null : new ItemId(value.Value)
        );
        builder.Property(x => x.TopicId)
            .HasConversion(arcIdConverter);
    }
}