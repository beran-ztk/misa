using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Audit;

namespace Misa.Infrastructure.Configurations.Ef.Audit;

public class SessionSegmentEf : IEntityTypeConfiguration<SessionSegment>
{
    public void Configure(EntityTypeBuilder<SessionSegment> builder)
    {
        builder.ToTable("session_segments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.SessionId)
            .HasColumnName("session_id");

        builder.Property(x => x.PauseReason)
            .HasColumnName("pause_reason");

        
        builder.Property(x => x.StartedAtUtc)
            .HasColumnName("started_at_utc")
            .IsRequired();
        
        builder.Property(x => x.EndedAtUtc)
            .HasColumnName("ended_at_utc")
            .IsRequired();

    }
}