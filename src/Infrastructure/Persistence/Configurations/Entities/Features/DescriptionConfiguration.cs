using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Features.Descriptions;

namespace Misa.Infrastructure.Persistence.Configurations.Entities.Features;

public class DescriptionConfiguration : IEntityTypeConfiguration<Description>
{
    public void Configure(EntityTypeBuilder<Description> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .IsRequired();
        
        builder.Property(x => x.EntityId);
        
        builder.Property(x => x.Content)
            .IsRequired();
        
        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();
    }
}