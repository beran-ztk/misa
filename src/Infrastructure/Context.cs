using Microsoft.EntityFrameworkCore;
using Misa.Domain;

namespace Misa.Infrastructure;

public class Context(DbContextOptions<Context> options) : DbContext(options)
{
    public DbSet<Item> Items { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Context).Assembly);
    }
}