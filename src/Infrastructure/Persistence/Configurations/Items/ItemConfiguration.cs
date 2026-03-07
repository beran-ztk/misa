using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Items.Components.Chronicle.Journals;
using Misa.Domain.Items.Components.Schedules;
using Misa.Domain.Items.Components.Schola;
using Misa.Domain.Items.Components.Tasks;

namespace Misa.Infrastructure.Persistence.Configurations.Items;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");
        
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasConversion(i => i.Value, i => new ItemId(i))
            .ValueGeneratedNever();

        builder.Property(i => i.OwnerId)
            .HasMaxLength(128);
        
        builder.Property(i => i.Workflow);
        
        builder.Property(i => i.Title)
            .HasMaxLength(200);
        
        builder.Property(i => i.Description)
            .HasMaxLength(2000);
        
        builder.Property(i => i.IsDeleted)
            .HasDefaultValue(false);
        
        builder.Property(i => i.IsArchived)
            .HasDefaultValue(false);
        
        builder.Property(i => i.CreatedAt);
        
        builder.Property(i => i.ModifiedAt);
        
        // Relations
        builder.HasOne(i => i.Activity)
            .WithOne(a => a.Item)
            .HasForeignKey<ItemActivity>(a => a.Id)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(i => i.TaskExtension)
            .WithOne()
            .HasForeignKey<TaskExtension>(t => t.Id)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(i => i.ScheduleExtension)
            .WithOne()
            .HasForeignKey<ScheduleExtension>(s => s.Id)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(i => i.JournalExtension)
            .WithOne()
            .HasForeignKey<JournalExtension>(j => j.Id)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(i => i.Arc)
            .WithOne()
            .HasForeignKey<Arc>(a => a.Id)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(i => i.Unit)
            .WithOne(u => u.Item)
            .HasForeignKey<Unit>(u => u.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}