using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;
using Misa.Infrastructure.Persistence.Context;
using State = Misa.Domain.Features.Entities.Extensions.Items.Base.State;

namespace Misa.Infrastructure.Persistence.Repositories;

public class MainRepository(DefaultContext db) : IMainRepository
{
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