using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Audit;

namespace Misa.Infrastructure.Configurations.Ef.Audit;

public class ActionTypeEf : IEntityTypeConfiguration<ActionType>
{
    public void Configure(EntityTypeBuilder<ActionType> builder)
    {
        builder.ToTable("action_types");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(x => x.Synopsis)
            .HasColumnName("synopsis");

        builder.HasIndex(x => x.Name).IsUnique();
    }
}