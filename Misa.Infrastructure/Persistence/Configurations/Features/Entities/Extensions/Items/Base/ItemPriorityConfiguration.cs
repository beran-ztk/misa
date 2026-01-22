using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Extensions.Items.Base;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Base;

public class ItemPriorityConfiguration 
    : IEntityTypeConfiguration<Priority>
{
    public void Configure(EntityTypeBuilder<Priority> builder)
    {
        builder.ToTable("item_priorities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(x => x.Synopsis)
            .HasColumnName("synopsis");

        builder.Property(x => x.SortOrder)
            .IsRequired()
            .HasColumnName("sort_order");

        builder.HasData(
            new Priority(1, "None",     1, "Keine Priorität vergeben"),
            new Priority(2, "Low",      2, "Geringe Wichtigkeit"),
            new Priority(3, "Medium",   3, "Normale Priorität"),
            new Priority(4, "High",     4, "Wichtig; zeitnah bearbeiten"),
            new Priority(5, "Urgent",   5, "Dringend; sofortige Aktion"),
            new Priority(6, "Critical", 6, "Kritische Eskalation")
        );
    }
}
