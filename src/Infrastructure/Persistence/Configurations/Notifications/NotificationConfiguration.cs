using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Notifications;

namespace Misa.Infrastructure.Persistence.Configurations.Notifications;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .ValueGeneratedNever();

        builder.Property(n => n.Title)
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .HasMaxLength(2000);

        builder.Property(n => n.SourceKind);

        builder.Property(n => n.SourceId);

        builder.Property(n => n.CreatedAtUtc);

        builder.Property(n => n.DismissedAtUtc);

        builder.Property(n => n.ReadAtUtc);

        builder.Property(n => n.DeletedAtUtc);
    }
}
