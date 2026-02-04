using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Misa.Infrastructure.Persistence.Configurations.Features.User;

public class UserConfiguration : IEntityTypeConfiguration<Domain.Features.Users.User>
{
    public void Configure(EntityTypeBuilder<Domain.Features.Users.User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id);
        builder.Property(u => u.Username);
        builder.Property(u => u.Password);
        builder.Property(u => u.TimeZone);
    }
}