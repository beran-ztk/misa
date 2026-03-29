using Misa.Domain;

namespace Misa.Application;

public record GetTopicsRequest;

public sealed class GetItemHandler(Repository repository)
{
    // Create Topic
    public async Task<List<Item>> Handle(GetTopicsRequest getTopicsRequest)
    {
        var topics = await repository.GetTopicsAsync();
        return topics;
    }
}