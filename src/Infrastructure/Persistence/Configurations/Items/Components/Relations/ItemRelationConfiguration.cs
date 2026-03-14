using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Relations;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Relations;

public class ItemRelationConfiguration : IEntityTypeConfiguration<ItemRelation>
{
    public void Configure(EntityTypeBuilder<ItemRelation> builder)
    {
        builder.ToTable("ItemRelations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(r => r.SourceItemId)
            .HasConversion(s => s.Value, value => new ItemId(value))
            .IsRequired();

        builder.Property(r => r.TargetItemId)
            .HasConversion(s => s.Value, value => new ItemId(value))
            .IsRequired();

        builder.Property(r => r.RelationType)
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(r => r.SourceItem)
            .WithMany()
            .HasForeignKey(r => r.SourceItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.TargetItem)
            .WithMany()
            .HasForeignKey(r => r.TargetItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.SourceItemId);
        builder.HasIndex(r => r.TargetItemId);
    }
}
