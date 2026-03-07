using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Persistence;
using Misa.Contract.Items.Components.Zettelkasten;

namespace Misa.Application.Features.Items.Zettelkasten;

public sealed record GetTopicsCommand;

public class GetTopicsHandler(IItemRepository repository, ICurrentUser currentUser)
{
    public async Task<List<TopicListDto>> HandleAsync(GetTopicsCommand command)
    {
        var itemTopics = await repository.GetTopicsAsync();

        var topics = itemTopics.Select(i => new TopicListDto(i.Id.Value, i.Topic?.TopicId?.Value, i.Title)).ToList();
        return topics;
    }
}