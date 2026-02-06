using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

namespace Misa.Infrastructure.Persistence.Configurations.Entities.Extensions.Items.Features.Sessions;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.ItemId);

        builder.Property(x => x.State)
            .IsRequired()
            .HasDefaultValue(SessionState.Running);

        builder.Property(x => x.Efficiency)
            .IsRequired()
            .HasDefaultValue(SessionEfficiencyType.None);

        builder.Property(x => x.Concentration)
            .IsRequired()
            .HasDefaultValue(SessionConcentrationType.None);

        builder.Property(x => x.Objective);

        builder.Property(x => x.Summary);

        builder.Property(x => x.AutoStopReason);

        builder.Property(x => x.PlannedDuration);

        builder.Property(x => x.StopAutomatically)
            .IsRequired();
        
        builder.Property(x => x.WasAutomaticallyStopped);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        // Relations
        builder.HasMany(s => s.Segments)
            .WithOne()
            .HasForeignKey(s => s.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}