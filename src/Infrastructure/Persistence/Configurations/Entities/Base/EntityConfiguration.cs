using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Persistence.Configurations.Entities.Base;

public class EntityConfiguration : IEntityTypeConfiguration<Domain.Features.Entities.Base.Entity>
{
    public void Configure(EntityTypeBuilder<Domain.Features.Entities.Base.Entity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .IsRequired();
        
        builder.Property(x => x.OwnerId)
            .IsRequired();
        
        builder.Property(e => e.Workflow).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(e => e.IsArchived).HasDefaultValue(false).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.ArchivedAt);
        builder.Property(e => e.InteractedAt);
        
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