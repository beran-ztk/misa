using Misa.Domain;

namespace Misa.Application;

public record GetItemsRequest;

public sealed class GetItemHandler(Repository repository)
{
    public async Task<List<Item>> Handle(GetItemsRequest r)
    {
        return await repository.GetItemsAsync();
    }
}
