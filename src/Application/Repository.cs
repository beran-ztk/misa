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
    public async Task<Item?> GetItemAsync(Guid id)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        return await ctx.Items.FirstOrDefaultAsync(x => x.Id == id);
    }
    public async Task<bool> UpdateTitleAsync(Guid id, string title)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        
        var item = await ctx.Items.Where(x => x.Id == id).FirstOrDefaultAsync();
        if (item is null) return false;
        
        item.Title = title;
        await ctx.SaveChangesAsync();
        return true;
    }
    public async Task UpdateExpansionStateAsync(Guid id, bool isExpanded)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        
        var item = await ctx.Items.Where(x => x.Id == id).FirstOrDefaultAsync();
        if (item is null) return;
        
        item.IsExpanded = isExpanded;
        await ctx.SaveChangesAsync();
    }
}
