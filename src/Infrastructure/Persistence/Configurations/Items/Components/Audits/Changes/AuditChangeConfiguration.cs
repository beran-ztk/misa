using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Items.Components.Audits.Changes;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Audits.Changes;

public class AuditChangeConfiguration : IEntityTypeConfiguration<AuditChange>
{
    public void Configure(EntityTypeBuilder<AuditChange> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.SubjectId);

        builder.Property(x => x.ChangeType)
            .IsRequired();

        builder.Property(x => x.ValueBefore);

        builder.Property(x => x.ValueAfter);

        builder.Property(x => x.Reason);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();
    }
}