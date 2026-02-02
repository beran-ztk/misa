using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Messaging;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Messaging;

public class OutboxConfiguration : IEntityTypeConfiguration<Outbox>
{
    public void Configure(EntityTypeBuilder<Outbox> builder)
    {
        builder.HasKey(x => x.EventId);

        builder.Property(x => x.EventId)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.EventType)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();
    }
}