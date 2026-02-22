using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Sessions.Commands;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Sessions;

public static class SessionEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost(ActivityRoutes.StartSessionRoute, StartSession);
        api.MapPut(ActivityRoutes.PauseSessionRoute, PauseSession);
        api.MapPut(ActivityRoutes.ContinueSessionRoute, ContinueSession);
        api.MapPut(ActivityRoutes.StopSessionRoute, StopSession);
    }

    // Start
    private static async Task<IResult> StartSession(
        [FromRoute] Guid itemId,
        [FromBody] StartSessionDto dto,
        IMessageBus bus,
        CancellationToken ct)
    {
        var cmd = new StartSessionCommand(
            dto.ItemId,
            dto.PlannedDuration,
            dto.Objective,
            dto.StopAutomatically,
            dto.AutoStopReason
        );

        await bus.InvokeAsync(cmd, ct);
        return Results.Ok();
    }
    
    // Pause
    private static async Task<IResult> PauseSession(
        [FromRoute] Guid itemId,
        [FromBody] PauseSessionRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var cmd = new PauseSessionCommand(
            itemId,
            request.PauseReason
        );

        await bus.InvokeAsync(cmd, ct);
        return Results.Ok();
    }
    
    // Continue
    private static async Task<IResult> ContinueSession(
        [FromRoute] Guid itemId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var cmd = new ContinueSessionCommand(itemId);

        await bus.InvokeAsync(cmd, ct);
        return Results.Ok();
    }
    
    // Stop
    private static async Task<IResult> StopSession(
        [FromRoute] Guid itemId,
        [FromBody] StopSessionDto dto,
        IMessageBus bus,
        CancellationToken ct)
    {
        var cmd = new StopSessionCommand(
            dto.ItemId,
            dto.SessionEfficiency,
            dto.SessionConcentration,
            dto.Summary
        );

        await bus.InvokeAsync(cmd, ct);
        return Results.Ok();
    }
}