using Misa.Contract.Items.Lookups;
using Misa.Domain.Items;

namespace Misa.Application.Main.Repositories;

public interface IMainRepository
{
    public Task<List<State>> GetStatesForCreation(CancellationToken ct);
    public Task<List<Priority>> GetPriorities(CancellationToken ct);
    public Task<List<Category>> GetTaskCategories(CancellationToken ct);
}