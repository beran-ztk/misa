using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Persistence.Configurations;

public class Entity : IEntityTypeConfiguration<Domain.Entities.Entity>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Entity> builder)
    {
        builder.ToTable("entities", t =>
        {
            t.HasCheckConstraint("ck_entities_updated", "updated_at_utc IS NULL OR updated_at_utc >= created_at_utc");
            t.HasCheckConstraint("ck_entities_interacted", "interacted_at_utc IS NULL OR interacted_at_utc >= created_at_utc");
        });
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id");
        builder.Property(e => e.OwnerId)
            .HasColumnName("owner_id");
        builder.Property(e => e.WorkflowId)
            .HasColumnName("workflow_id")
            .IsRequired();
        
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
            .IsRequired();

        builder.HasQueryFilter(e => !e.IsDeleted);
        
        builder.HasOne(i => i.Workflow)
            .WithMany()
            .HasForeignKey(i => i.WorkflowId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(i => i.Item)
            .WithOne(i => i.Entity)
            .HasForeignKey<Domain.Items.Item>(i => i.EntityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Descriptions)
            .WithOne()
            .HasForeignKey(i => i.EntityId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(e => e.Sessions)
            .WithOne()
            .HasForeignKey(s => s.EntityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Actions)
            .WithOne()
            .HasForeignKey(a => a.EntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}