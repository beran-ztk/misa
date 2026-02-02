using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Task = Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Extensions.Tasks;

public sealed class TaskConfiguration : IEntityTypeConfiguration<Task>
{
    public void Configure(EntityTypeBuilder<Task> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id);
        
        builder.Property(x => x.Category)
            .IsRequired();

        builder.HasOne(x => x.Item)
            .WithOne()
            .HasForeignKey<Task>(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}