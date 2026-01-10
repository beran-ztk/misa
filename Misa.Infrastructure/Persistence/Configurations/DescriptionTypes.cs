using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Persistence.Configurations;

public class DescriptionTypes : IEntityTypeConfiguration<Domain.Extensions.DescriptionTypes>
{
    public void Configure(EntityTypeBuilder<Domain.Extensions.DescriptionTypes> builder)
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
    }
}