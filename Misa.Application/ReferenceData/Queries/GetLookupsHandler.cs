using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Features.Entities.Extensions.Items.Base.Mappings;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Mappings;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Lookups;
using Misa.Domain.Features.Entities.Extensions.Items.Base;

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
            TaskCategories = (await repository.GetTaskCategories(ct)).ToDto(),
            EfficiencyTypes = (await repository.GetEfficiencyTypes(ct)).ToDto(),
            ConcentrationTypes = (await repository.GetConcentrationTypes(ct)).ToDto()
        };   
    }
}