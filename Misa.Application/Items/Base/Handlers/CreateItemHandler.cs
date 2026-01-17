using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Entities.Mappings;
using Misa.Application.Items.Mappings;
using Misa.Contract.Entities;
using Misa.Contract.Items.Common;
using Misa.Domain.Dictionaries.Items;

namespace Misa.Application.Items.Base.Handlers;

public class CreateItemHandler(IItemRepository repository)
{
    public async Task<ReadItemDto> AddTaskAsync(
        CreateItemDto itemDto,
        CancellationToken ct = default)
    {
        var entityDto = new CreateEntityDto
        {
            OwnerId = itemDto.OwnerId,
            WorkflowId = (int)Misa.Domain.Dictionaries.Entities.EntityWorkflows.Task
        };

        itemDto.StateId = (int)ItemStates.Draft;
        var entity = entityDto.ToDomain();
        var item = itemDto.ToDomain(entity);
        
        var createdItem = await repository.AddAsync(item, ct);
        return createdItem.ToReadItemDto();
    }
}