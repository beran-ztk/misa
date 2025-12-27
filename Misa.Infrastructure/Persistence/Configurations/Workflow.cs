using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Configurations.Ef;

public class Workflow : IEntityTypeConfiguration<Misa.Domain.Entities.Workflow>
{
    public void Configure(EntityTypeBuilder<Misa.Domain.Entities.Workflow> builder)
    {
        builder.ToTable("entity_workflow_types");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id");
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasColumnName("name");
        
        builder.Property(x => x.Synopsis)
            .HasColumnName("synopsis");
    }
}