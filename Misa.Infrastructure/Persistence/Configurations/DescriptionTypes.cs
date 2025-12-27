using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Configurations.Ef;

public class DescriptionTypes : IEntityTypeConfiguration<Misa.Domain.Main.DescriptionTypes>
{
    public void Configure(EntityTypeBuilder<Misa.Domain.Main.DescriptionTypes> builder)
    {
        builder.ToTable("description_types");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id");
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasColumnName("name");
        
        builder.Property(x => x.Synopsis)
            .HasColumnName("synopsis");
        
        builder.Property(x => x.SortOrder)
            .IsRequired()
            .HasColumnName("sort_order");
    }
}