using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain;

namespace Misa.Infrastructure.Configurations;

public sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("notes");

        builder.HasKey(x => x.ItemId);

        builder.Property(x => x.ItemId)
            .ValueGeneratedNever();

        builder.Property(x => x.Content);
    }
}