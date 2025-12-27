using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Configurations.Ef;

public class Category : IEntityTypeConfiguration<Misa.Domain.Items.Category>
{
    public void Configure(EntityTypeBuilder<Misa.Domain.Items.Category> builder)
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
        
        builder.HasOne(x => x.Workflow)
            .WithMany()
            .HasForeignKey(x => x.WorkflowId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}