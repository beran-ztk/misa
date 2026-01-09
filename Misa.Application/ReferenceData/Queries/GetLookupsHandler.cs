using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Items.Mappings;
using Misa.Contract.Items.Lookups;
using Misa.Contract.Main;
using Misa.Domain.Dictionaries.Items;

namespace Misa.Application.ReferenceData.Queries;

public class GetLookupsHandler(IMainRepository repository)
{
    public async Task<List<StateDto>> GetUserSettableStates(int stateId, CancellationToken ct = default)
    {
        var currentState = StateTransitions.GetEnumFromId(stateId);
        var allowedStates = StateTransitions.From(currentState);
        var states= await repository.GetStatesByIds(allowedStates, ct);
        
        return states.ToDto();
    }
    public async Task<LookupsDto> GetAllAsync(CancellationToken ct)
    {
        return new LookupsDto
        {
            Priorities = (await repository.GetPriorities(ct)).ToDto(),
            TaskCategories = (await repository.GetTaskCategories(ct)).ToDto(),
            EfficiencyTypes = (await repository.GetEfficiencyTypes(ct)).ToDto(),
            ConcentrationTypes = (await repository.GetConcentrationTypes(ct)).ToDto()
        };   
    }
}