using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Base;

public class EntityWorkflowConfiguration : IEntityTypeConfiguration<Domain.Features.Entities.Base.Workflow>
{
    public void Configure(EntityTypeBuilder<Domain.Features.Entities.Base.Workflow> builder)
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