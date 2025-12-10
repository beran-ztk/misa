using Misa.Application.Items.Mappings;
using Misa.Application.Main.Repositories;
using Misa.Contract.Main;

namespace Misa.Application.Main.Get;

public class GetLookupsHandler(IMainRepository repository)
{
    public async Task<LookupsDto> GetAllAsync(CancellationToken ct)
    => new LookupsDto
        {
            States = (await repository.GetStatesForCreation(ct)).ToDto(),
            Priorities = (await repository.GetPriorities(ct)).ToDto(),
            TaskCategories = (await repository.GetTaskCategories(ct)).ToDto()
        };
}