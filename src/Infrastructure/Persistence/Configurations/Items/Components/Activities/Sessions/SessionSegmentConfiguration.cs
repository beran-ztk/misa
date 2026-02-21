using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Items.Components.Activities.Sessions;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Activities.Sessions;

public class SessionSegmentConfiguration : IEntityTypeConfiguration<SessionSegment>
{
    public void Configure(EntityTypeBuilder<SessionSegment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.SessionId);

        builder.Property(x => x.PauseReason);
        
        builder.Property(x => x.StartedAtUtc)
            .IsRequired();
        
        builder.Property(x => x.EndedAtUtc);
    }
}