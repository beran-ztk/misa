using Misa.Domain.Features.Entities.Extensions.Items.Base;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface IMainRepository
{
    public Task<List<State>> GetStatesByIds(IReadOnlyCollection<ItemStates> states, CancellationToken ct = default);
}