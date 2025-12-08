using Misa.Application.Entities.Mappings;
using Misa.Application.Items.Mappings;
using Misa.Application.Items.Repositories;
using Misa.Contract.Entities;
using Misa.Contract.Items;

namespace Misa.Application.Items.Add;

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
            
        var entity = entityDto.ToDomain();
        var item = itemDto.ToDomain(entity);
        
        var createdItem = await repository.AddAsync(item, ct);
        return createdItem.ToReadItemDto();
    }
}