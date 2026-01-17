using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Features.Sessions;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ItemId)
            .HasColumnName("item_id");

        builder.Property(x => x.StateId)
            .HasColumnName("state_id");
        
        builder.Property(x => x.EfficiencyId)
            .HasColumnName("efficiency_id");

        builder.Property(x => x.ConcentrationId)
            .HasColumnName("concentration_id");

        builder.Property(x => x.Objective)
            .HasColumnName("objective");

        builder.Property(x => x.Summary)
            .HasColumnName("summary");

        builder.Property(x => x.AutoStopReason)
            .HasColumnName("auto_stop_reason");

        builder.Property(x => x.PlannedDuration)
            .HasColumnName("planned_duration")
            .HasColumnType("interval");

        builder.Property(x => x.StopAutomatically)
            .HasColumnName("stop_automatically")
            .IsRequired();
        
        builder.Property(x => x.WasAutomaticallyStopped)
            .HasColumnName("was_automatically_stopped");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasOne(x => x.State)
            .WithMany()
            .HasForeignKey(x => x.StateId)
            .OnDelete(DeleteBehavior.Restrict); 
        
        builder.HasOne(x => x.Efficiency)
            .WithMany()
            .HasForeignKey(x => x.EfficiencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Concentration)
            .WithMany()
            .HasForeignKey(x => x.ConcentrationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Segments)
            .WithOne()
            .HasForeignKey(s => s.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}