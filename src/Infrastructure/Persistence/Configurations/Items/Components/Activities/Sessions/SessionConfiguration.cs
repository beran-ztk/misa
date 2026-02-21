using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities.Sessions;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Activities.Sessions;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ItemId)
            .HasConversion(i => i.Value, value => new ItemId(value));

        builder.Property(x => x.State)
            .HasDefaultValue(SessionState.Running);

        builder.Property(x => x.Efficiency)
            .HasDefaultValue(SessionEfficiencyType.None);

        builder.Property(x => x.Concentration)
            .HasDefaultValue(SessionConcentrationType.None);

        builder.Property(x => x.Objective);

        builder.Property(x => x.Summary);

        builder.Property(x => x.AutoStopReason);

        builder.Property(x => x.PlannedDuration);

        builder.Property(x => x.StopAutomatically)
            .IsRequired();
        
        builder.Property(x => x.WasAutomaticallyStopped);

        builder.Property(x => x.CreatedAtUtc);

        // Relations
        builder.HasMany(s => s.Segments)
            .WithOne()
            .HasForeignKey(s => s.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}