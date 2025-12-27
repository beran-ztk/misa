using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Audit;

namespace Misa.Infrastructure.Configurations.Ef.Audit;

public class SessionEfficiencyTypeEf : IEntityTypeConfiguration<SessionEfficiencyType>
{
    public void Configure(EntityTypeBuilder<SessionEfficiencyType> builder)
    {
        builder.ToTable("session_efficiency_types");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(x => x.Synopsis)
            .HasColumnName("synopsis");

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.SortOrder).IsUnique();
    }
}