using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Audit;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Audit;

public class AuditChangeConfiguration : IEntityTypeConfiguration<AuditChange>
{
    public void Configure(EntityTypeBuilder<AuditChange> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.EntityId);

        builder.Property(x => x.ChangeType)
            .IsRequired();

        builder.Property(x => x.ValueBefore);

        builder.Property(x => x.ValueAfter);

        builder.Property(x => x.Reason);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();
    }
}