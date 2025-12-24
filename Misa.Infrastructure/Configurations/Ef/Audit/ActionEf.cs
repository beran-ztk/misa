using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Configurations.Ef.Audit;

public class ActionEf : IEntityTypeConfiguration<Misa.Domain.Audit.Action>
{
    public void Configure(EntityTypeBuilder<Misa.Domain.Audit.Action> builder)
    {
        builder.ToTable("actions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id");

        builder.Property(x => x.TypeId)
            .HasColumnName("type_id")
            .IsRequired();

        builder.Property(x => x.ValueBefore)
            .HasColumnName("value_before");

        builder.Property(x => x.ValueAfter)
            .HasColumnName("value_after");

        builder.Property(x => x.Reason)
            .HasColumnName("reason");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        // FK to lookup
        builder.HasOne(x => x.Type)
            .WithMany()
            .HasForeignKey(x => x.TypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}