using Microsoft.EntityFrameworkCore;
using Misa.Application.Main.Repositories;
using Misa.Contract.Items.Lookups;
using Misa.Domain.Items;
using Misa.Infrastructure.Data;

namespace Misa.Infrastructure.Main;

public class MainRepository(MisaDbContext db) : IMainRepository
{
    public async Task<List<State>> GetStatesForCreation(CancellationToken ct)
        => await db.States
            .Where(s 
                => s.Id == (int)Misa.Domain.Dictionaries.Items.ItemStates.Draft
                || s.Id == (int)Misa.Domain.Dictionaries.Items.ItemStates.Open
                || s.Id == (int)Misa.Domain.Dictionaries.Items.ItemStates.Done
                || s.Id == (int)Misa.Domain.Dictionaries.Items.ItemStates.Archived)
            .ToListAsync(ct);

    public async Task<List<Priority>> GetPriorities(CancellationToken ct)
        => await db.Priorities.ToListAsync(ct);

    public async Task<List<Category>> GetTaskCategories(CancellationToken ct)
        => await db.Categories.ToListAsync(ct);
}