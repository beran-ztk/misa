using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Task = Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task;

namespace Misa.Infrastructure.Persistence.Configurations.Features.Entities.Extensions.Items.Extensions.Tasks;

public sealed class TaskConfiguration : IEntityTypeConfiguration<Task>
{
    public void Configure(EntityTypeBuilder<Task> builder)
    {
        builder.ToTable("tasks");
        
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");
        
        builder.Property(x => x.Category)
            .HasColumnName("category")
            .HasColumnType("task_category")
            .IsRequired();

        builder.HasOne(x => x.Item)
            .WithOne()
            .HasForeignKey<Task>(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}