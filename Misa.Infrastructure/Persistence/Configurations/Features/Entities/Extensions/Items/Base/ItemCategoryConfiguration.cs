using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Base;

public class ItemCategoryConfiguration : IEntityTypeConfiguration<Domain.Features.Entities.Extensions.Items.Base.Category>
{
    public void Configure(EntityTypeBuilder<Domain.Features.Entities.Extensions.Items.Base.Category> builder)
    {
        builder.ToTable("workflow_category_types");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id");
        
        builder.Property(x => x.WorkflowId)
            .IsRequired()
            .HasColumnName("workflow_id");
        
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