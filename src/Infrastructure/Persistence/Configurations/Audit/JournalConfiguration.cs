using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Audit;

namespace Misa.Infrastructure.Persistence.Configurations.Audit;

public sealed class JournalConfiguration : IEntityTypeConfiguration<Journal>
{
    public void Configure(EntityTypeBuilder<Journal> builder)
    {
        builder.ToTable("journals");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(v => v.Value, v => new JournalId(v))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Journal (1) → User (1)
        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<Journal>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Journal (1) -> Entries (N)
        builder.HasMany(x => x.Entries)
            .WithOne()
            .HasForeignKey(j => j.JournalId)
            .OnDelete(DeleteBehavior.Restrict);

        // Journal (1) -> Categories (N)
        builder.HasMany(x => x.Categories)
            .WithOne()
            .HasForeignKey(c => c.JournalId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1 Journal pro User
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}

public sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("journal_entries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(v => v.Value, v => new JournalEntryId(v))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.JournalId)
            .HasConversion(v => v.Value, v => new JournalId(v))
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.OccurredAt)
            .IsRequired();

        builder.Property(x => x.UntilAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.OriginId);

        builder.Property(x => x.SystemType)
            .IsRequired(false);

        builder.Property(x => x.CategoryId)
            .HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                db => db.HasValue ? new JournalCategoryId(db.Value) : null
            )
            .IsRequired(false);
    }
}

public sealed class JournalCategoryConfiguration : IEntityTypeConfiguration<JournalCategory>
{
    public void Configure(EntityTypeBuilder<JournalCategory> builder)
    {
        builder.ToTable("journal_categories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                v => v.Value,
                db => new JournalCategoryId(db))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.JournalId)
            .HasConversion(v => v.Value, v => new JournalId(v))
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Unique pro Journal
        builder.HasIndex(j => new { j.JournalId, j.Name })
            .IsUnique();
    }
}
