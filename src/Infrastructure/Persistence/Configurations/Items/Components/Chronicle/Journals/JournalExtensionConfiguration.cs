using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Chronicle.Journals;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Chronicle.Journals;

public sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalExtension>
{
    public void Configure(EntityTypeBuilder<JournalExtension> builder)
    {
        builder.ToTable("item_journals");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(v => v.Value, v => new ItemId(v))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.OccurredAt);

        builder.Property(x => x.UntilAt);
    }
}
