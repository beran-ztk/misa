using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Audit;
using Misa.Domain.Dictionaries.Items;
using Misa.Domain.Extensions;
using Misa.Infrastructure.Data;
using Category = Misa.Domain.Items.Category;
using Priority = Misa.Domain.Items.Priority;
using State = Misa.Domain.Items.State;

namespace Misa.Infrastructure.Persistence.Repositories;

public class MainRepository(MisaDbContext db) : IMainRepository
{
    public async Task AddDescriptionAsync(Description description)
    {
        await db.Descriptions.AddAsync(description);
        await db.SaveChangesAsync();
    }
    public async Task<List<Priority>> GetPriorities(CancellationToken ct)
        => await db.Priorities.ToListAsync(ct);
    public async Task<List<Category>> GetTaskCategories(CancellationToken ct)
        => await db.Categories.ToListAsync(ct);
    public async Task<List<SessionEfficiencyType>> GetEfficiencyTypes(CancellationToken ct)
        => await db.EfficiencyTypes.ToListAsync(ct);
    public async Task<List<SessionConcentrationType>> GetConcentrationTypes(CancellationToken ct)
        => await db.ConcentrationTypes.ToListAsync(ct);

    public async Task<List<State>> GetStatesByIds(
        IReadOnlyCollection<ItemStates> states, 
        CancellationToken ct = default)
    {
        var ids = states.Select(s => (int)s).ToArray();
        return await db.States
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(ct);
    }
}