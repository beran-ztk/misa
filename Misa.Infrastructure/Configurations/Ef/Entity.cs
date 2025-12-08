using Misa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Misa.Infrastructure.Configurations.Ef;

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
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at_utc")
            .IsRequired(false);
        builder.Property(e => e.InteractedAt)
            .HasColumnName("interacted_at_utc")
            .IsRequired();

        builder.HasQueryFilter(e => !e.IsDeleted);
        
        builder.HasIndex(e => e.OwnerId)
            .HasDatabaseName("ix_entities_owner");
        builder.HasIndex(e => e.WorkflowId)
            .HasDatabaseName("ix_entities_workflow");
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_entities_created");
        
        builder.HasOne(i => i.Workflow)
            .WithMany()
            .HasForeignKey(i => i.WorkflowId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}