using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Entities.Extensions;

namespace Misa.Infrastructure.Persistence.Configurations.Entity;

public class DescriptionEf : IEntityTypeConfiguration<Description>
{
    public void Configure(EntityTypeBuilder<Description> builder)
    {
        builder.ToTable("descriptions");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id");
        
        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id");
        
        builder.Property(x => x.Content)
            .IsRequired()
            .HasColumnName("content");
        
        builder.Property(x => x.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at_utc");
    }
}