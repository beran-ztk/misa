using Misa.Application.Items.Mappings;
using Misa.Application.Items.Repositories;
using Misa.Contract.Items;

namespace Misa.Application.Items.Get;

public class GetItemsHandler(IItemRepository repository)
{
    public async Task<ReadItemDto?> GetTaskAsync(Guid id, CancellationToken ct)
    {
        var item = await repository.GetTaskAsync(id, ct);
        return item?.ToReadItemDto();
    }
    public async Task<List<ReadItemDto>> GetTasksAsync(CancellationToken ct)
        => (await repository.GetAllTasksAsync(ct)).ToReadItemDto();
}