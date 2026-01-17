using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Base;

public class ItemPriorityConfiguration : IEntityTypeConfiguration<Domain.Features.Entities.Extensions.Items.Base.Priority>
{
    public void Configure(EntityTypeBuilder<Domain.Features.Entities.Extensions.Items.Base.Priority> builder)
    {
        builder.ToTable("item_priorities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id");
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasColumnName("name");
        
        builder.Property(x => x.Synopsis)
            .HasColumnName("synopsis");
        
        builder.Property(x => x.SortOrder)
            .IsRequired()
            .HasColumnName("sort_order");
    }
}