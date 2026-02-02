using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Features.Descriptions;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Features.Descriptions;

public class DescriptionConfiguration : IEntityTypeConfiguration<Description>
{
    public void Configure(EntityTypeBuilder<Description> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(x => x.EntityId);
        
        builder.Property(x => x.Content)
            .IsRequired();
        
        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();
    }
}