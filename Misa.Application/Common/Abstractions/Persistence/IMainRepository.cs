using Misa.Domain.Audit;
using Misa.Domain.Dictionaries.Items;
using Misa.Domain.Extensions;
using Misa.Domain.Items;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface IMainRepository
{
    public Task AddDescriptionAsync(Description description);
    public Task<List<Priority>> GetPriorities(CancellationToken ct);
    public Task<List<Category>> GetTaskCategories(CancellationToken ct);
    public Task<List<SessionEfficiencyType>> GetEfficiencyTypes(CancellationToken ct);
    public Task<List<SessionConcentrationType>> GetConcentrationTypes(CancellationToken ct);
    public Task<List<State>> GetStatesByIds(IReadOnlyCollection<ItemStates> states, CancellationToken ct = default);
}