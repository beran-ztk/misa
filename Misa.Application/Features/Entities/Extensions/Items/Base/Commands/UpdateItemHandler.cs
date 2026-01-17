using Misa.Application.Common.Abstractions.Persistence;
using Misa.Contract.Features.Entities.Extensions.Items.Base;

namespace Misa.Application.Features.Entities.Extensions.Items.Base.Commands;

public class UpdateItemHandler(IItemRepository repository)
{
    public async System.Threading.Tasks.Task UpdateAsync(UpdateItemDto dto)
    {
        var item = await repository.TryGetItemAsync(dto.EntityId, CancellationToken.None);
        var hasBeenChanged = false;
        
        // if (dto.Title != null)
        //     item.Rename(dto.Title, ref hasBeenChanged);
        //
        // if (dto.StateId != null)
        //     item.ChangeState((int)dto.StateId, ref hasBeenChanged);
        //
        // if (dto.PriorityId.HasValue)
        //     item.ChangePriority((int)dto.PriorityId, ref hasBeenChanged);
        //
        // if (dto.CategoryId.HasValue)
        //     item.ChangeCategory((int)dto.CategoryId, ref hasBeenChanged);

        if (!hasBeenChanged)
            return;

        item.Entity.Update();
        await repository.SaveChangesAsync();
    }
}