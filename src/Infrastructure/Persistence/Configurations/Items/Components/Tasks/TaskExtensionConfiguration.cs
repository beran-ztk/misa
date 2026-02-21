using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Tasks;

namespace Misa.Infrastructure.Persistence.Configurations.Items.Components.Tasks;

public sealed class TaskExtensionConfiguration : IEntityTypeConfiguration<TaskExtension>
{
    public void Configure(EntityTypeBuilder<TaskExtension> builder)
    {
        builder.ToTable("item_tasks");
        
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ItemId(value))
            .ValueGeneratedNever();
        
        builder.Property(x => x.Category);
    }
}