using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Configurations.Ef;

public class State : IEntityTypeConfiguration<Misa.Domain.Items.State>
{
    public void Configure(EntityTypeBuilder<Misa.Domain.Items.State> builder)
    {
        builder.ToTable("item_states");
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