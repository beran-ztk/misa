using Misa.Domain;
using Misa.Infrastructure;

namespace Misa.Core;

public sealed class Repository(Context context)
{
    // Handle Context
    public async Task SaveChangesAsync() => await context.SaveChangesAsync();

    // Add item
    public async Task AddAsync(Item item) => await context.Items.AddAsync(item);
}