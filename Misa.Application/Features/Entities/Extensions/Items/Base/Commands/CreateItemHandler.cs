using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Features.Entities.Base.Mappings;
using Misa.Application.Features.Entities.Extensions.Items.Base.Mappings;
using Misa.Contract.Features.Entities.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;

namespace Misa.Application.Features.Entities.Extensions.Items.Base.Commands;

public class CreateItemHandler(IItemRepository repository)
{
    public async Task<ReadItemDto> AddTaskAsync(
        CreateItemDto itemDto,
        CancellationToken ct = default)
    {
        var entityDto = new CreateEntityDto
        {
            OwnerId = itemDto.OwnerId,
            WorkflowId = (int)Workflow.Task
        };

        itemDto.StateId = (int)ItemStates.Draft;
        var entity = entityDto.ToDomain();
        var item = itemDto.ToDomain(entity);
        
        var createdItem = await repository.AddAsync(item, ct);
        return createdItem.ToReadItemDto();
    }
}