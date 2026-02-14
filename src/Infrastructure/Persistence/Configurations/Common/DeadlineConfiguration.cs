using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Common;

namespace Misa.Infrastructure.Persistence.Configurations.Common;

public class DeadlineConfiguration : IEntityTypeConfiguration<Deadline>
{
    public void Configure(EntityTypeBuilder<Deadline> builder)
    {
        builder.HasKey(x => x.ItemId);

        builder.Property(x => x.ItemId)
            .ValueGeneratedNever()
            .IsRequired();
        
        builder.Property(e => e.DueAt).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
    }
}

