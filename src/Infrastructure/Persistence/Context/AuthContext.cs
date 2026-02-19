using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Misa.Infrastructure.Auth;

namespace Misa.Infrastructure.Persistence.Context;

public sealed class AuthContext(DbContextOptions<AuthContext> options)
    : IdentityDbContext<User>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(b =>
        {
            b.Property(u => u.TimeZone)
                .HasMaxLength(100)
                .IsRequired();
        });
    }
}
