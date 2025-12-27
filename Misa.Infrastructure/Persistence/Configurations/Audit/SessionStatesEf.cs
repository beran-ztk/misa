using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Audit;

namespace Misa.Infrastructure.Configurations.Ef.Audit;

public class SessionStatesEf : IEntityTypeConfiguration<SessionStates>
{
    public void Configure(EntityTypeBuilder<SessionStates> builder)
    {
        builder.ToTable("session_states");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(x => x.Synopsis)
            .HasColumnName("synopsis");

        builder.HasIndex(x => x.Name)
            .IsUnique();
    }
}