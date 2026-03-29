using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain;

namespace Misa.Infrastructure.Configurations;

public class QuestConfiguration : IEntityTypeConfiguration<Quest>
{
    public void Configure(EntityTypeBuilder<Quest> builder)
    {
        builder.ToTable("quests");

        builder.HasKey(x => x.ItemId);

        builder.Property(x => x.ItemId)
            .ValueGeneratedNever();

        builder.Property(x => x.State);
    }
}