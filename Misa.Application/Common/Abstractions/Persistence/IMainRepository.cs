using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface IMainRepository
{
    public Task<List<Category>> GetTaskCategories(CancellationToken ct);
    public Task<List<SessionEfficiencyType>> GetEfficiencyTypes(CancellationToken ct);
    public Task<List<SessionConcentrationType>> GetConcentrationTypes(CancellationToken ct);
    public Task<List<State>> GetStatesByIds(IReadOnlyCollection<ItemStates> states, CancellationToken ct = default);
}