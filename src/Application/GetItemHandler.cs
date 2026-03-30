using Misa.Domain;

namespace Misa.Application;

public record GetItemsRequest;
public record GetItemRequest(Guid Id);

public sealed class GetItemHandler(Repository repository)
{
    public async Task<List<Item>> Handle(GetItemsRequest r)
    {
        return await repository.GetItemsAsync();
    }

    public async Task<Item?> Handle(GetItemRequest r)
    {
        return await repository.GetItemAsync(r.Id);
    }
}
