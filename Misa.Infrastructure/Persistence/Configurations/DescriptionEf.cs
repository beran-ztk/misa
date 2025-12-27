using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Configurations.Ef;

public class DescriptionEf : IEntityTypeConfiguration<Misa.Domain.Main.Description>
{
    public void Configure(EntityTypeBuilder<Misa.Domain.Main.Description> builder)
    {
        builder.ToTable("descriptions");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id");
        
        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id");
        
        builder.Property(x => x.TypeId)
            .IsRequired()
            .HasColumnName("type_id");
        
        builder.Property(x => x.Content)
            .IsRequired()
            .HasColumnName("content");
        
        builder.Property(x => x.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at_utc");
        
        builder.HasOne(x => x.Type)
            .WithMany()
            .HasForeignKey(x => x.TypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}