using Misa.Application.Common.Abstractions.Persistence;
using Misa.Contract.Items;

namespace Misa.Application.Items.Commands;

public class UpdateItemHandler(IItemRepository repository)
{
    public async Task UpdateAsync(UpdateItemDto dto)
    {
        var item = await repository.TryGetItemAsync(dto.EntityId);
        var hasBeenChanged = false;
        
        if (dto.Title != null)
            item.Rename(dto.Title, ref hasBeenChanged);
        
        if (dto.StateId != null)
            item.ChangeState((int)dto.StateId, ref hasBeenChanged);
        
        if (dto.PriorityId.HasValue)
            item.ChangePriority((int)dto.PriorityId, ref hasBeenChanged);
        
        if (dto.CategoryId.HasValue)
            item.ChangeCategory((int)dto.CategoryId, ref hasBeenChanged);

        if (!hasBeenChanged)
            return;

        item.Entity.Update();
        await repository.SaveChangesAsync();
    }
}