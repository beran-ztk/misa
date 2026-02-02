using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Features.Sessions;

public class SessionSegmentConfiguration : IEntityTypeConfiguration<SessionSegment>
{
    public void Configure(EntityTypeBuilder<SessionSegment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.SessionId);

        builder.Property(x => x.PauseReason);
        
        builder.Property(x => x.StartedAtUtc)
            .IsRequired();
        
        builder.Property(x => x.EndedAtUtc);
    }
}