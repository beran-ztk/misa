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
    public async Task<List<Item>> GetItemsAsync()
    {
        await using var ctx = await factory.CreateDbContextAsync();
        return await ctx.Items.ToListAsync();
    }
}
