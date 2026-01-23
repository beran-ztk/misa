using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Base;

public class EntityConfiguration : IEntityTypeConfiguration<Domain.Features.Entities.Base.Entity>
{
    public void Configure(EntityTypeBuilder<Domain.Features.Entities.Base.Entity> builder)
    {
        builder.ToTable("entities");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(e => e.Workflow)
            .IsRequired()
            .HasColumnName("workflow")
            .HasColumnType("workflow");
        
        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();
        builder.Property(e => e.IsArchived)
            .HasColumnName("is_archived")
            .HasDefaultValue(false)
            .IsRequired();
        
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at_utc")
            .IsRequired(false);
        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at_utc")
            .IsRequired(false);
        builder.Property(e => e.ArchivedAt)
            .HasColumnName("archived_at_utc")
            .IsRequired(false);
        builder.Property(e => e.InteractedAt)
            .HasColumnName("interacted_at_utc")
            .IsRequired(false);
        
        // Relations
        builder.HasMany(i => i.Descriptions)
            .WithOne()
            .HasForeignKey(i => i.EntityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Changes)
            .WithOne()
            .HasForeignKey(a => a.EntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}