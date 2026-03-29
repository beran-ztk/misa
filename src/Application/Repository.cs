using Microsoft.EntityFrameworkCore;
using Misa.Domain;
using Misa.Infrastructure;

namespace Misa.Application;

public sealed class Repository(IDbContextFactory<Context> factory)
{
    public async Task AddAsync(Item item)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        ctx.Items.Add(item);
        await ctx.SaveChangesAsync();
    }
    public async Task<List<Item>> GetTopicsAsync()
    {
        await using var ctx = await factory.CreateDbContextAsync();
        var topics = await ctx.Items
            .Where(i => i.Kind == Kind.Topic)
            .ToListAsync();
        return topics;
    }
}
