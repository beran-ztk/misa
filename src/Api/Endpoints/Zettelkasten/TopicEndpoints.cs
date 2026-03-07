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
        api.MapGet(ZettelkastenRoutes.GetTopics, GetTopics);
    }
    
    // Create a task
    private static async Task<IResult> CreateTopic(
        [FromBody] CreateTopicRequest request, 
        IMessageBus bus)
    {
        var command = new CreateTopicCommand(
            request.Title,
            request.ParentId
        );

        await bus.InvokeAsync(command);
        return Results.Ok();
    }  
    
    private static async Task<IResult> GetTopics(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<List<TopicListDto>>(new GetTopicsCommand());
        return Results.Ok(result);
    }  
}