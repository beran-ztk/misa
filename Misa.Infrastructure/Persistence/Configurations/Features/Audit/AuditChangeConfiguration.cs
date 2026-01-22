using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Audit;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Audit;

public class AuditChangeConfiguration : IEntityTypeConfiguration<AuditChange>
{
    public void Configure(EntityTypeBuilder<AuditChange> builder)
    {
        builder.ToTable("audit_changes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id");

        builder.Property(x => x.ChangeType)
            .IsRequired()
            .HasColumnName("field")
            .HasColumnType("change_type");

        builder.Property(x => x.ValueBefore)
            .HasColumnName("value_before");

        builder.Property(x => x.ValueAfter)
            .HasColumnName("value_after");

        builder.Property(x => x.Reason)
            .HasColumnName("reason");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
    }
}