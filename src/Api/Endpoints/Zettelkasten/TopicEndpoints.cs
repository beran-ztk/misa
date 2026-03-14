using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Zettelkasten;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Zettelkasten;

public class TopicEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost(ZettelkastenRoutes.CreateTopic, CreateTopic);
        api.MapGet(ZettelkastenRoutes.GetTopics, GetKnowledgeIndex);
    }

    private static async Task<IResult> CreateTopic(
        [FromBody] CreateTopicRequest request,
        IMessageBus bus)
    {
        var command = new CreateTopicCommand(request.Title, request.ParentId);
        await bus.InvokeAsync(command);
        return Results.Ok();
    }

    private static async Task<IResult> GetKnowledgeIndex(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<List<KnowledgeIndexEntryDto>>(new GetKnowledgeIndexQuery(), ct);
        return Results.Ok(result);
    }
}
