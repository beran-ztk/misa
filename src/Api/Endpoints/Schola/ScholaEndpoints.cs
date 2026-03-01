using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Schola;
using Misa.Contract.Items.Components.Schola;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Schola;

public static class ScholaEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost(ScholaRoutes.CreateArc, CreateArc);
        api.MapPost(ScholaRoutes.CreateUnit, CreateUnit);
    }
    
    // Create a task
    private static async Task<IResult> CreateArc(
        [FromBody] CreateArcRequest request, 
        IMessageBus bus)
    {
        var command = new CreateArcCommand(
            request.Title,
            request.Description,
            request.ActivityPriorityDto,
            request.Objective
        );

        await bus.InvokeAsync(command);
        return Results.Ok();
    }
    
    private static async Task<IResult> CreateUnit(
        [FromBody] CreateUnitRequest request, 
        IMessageBus bus)
    {
        var command = new CreateUnitCommand(
            request.Title,
            request.Description,
            request.ActivityPriorityDto,
            request.Objective
        );

        await bus.InvokeAsync(command);
        return Results.Ok();
    }
}