using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Base.Queries;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Queries;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints;

public static class ItemDetailEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("items/{itemId:guid}/details", GetDetails);
        app.MapGet("items/{itemId:guid}/overview/session", GetCurrentSessionDetails);
        app.MapPost("items/{itemId:guid}/sessions/start", StartSession);
        app.MapPost("items/{itemId:guid}/sessions/pause", PauseSession);
        app.MapPost("items/{itemId:guid}/sessions/continue", ContinueSession);
        app.MapPost("items/{itemId:guid}/sessions/stop", StopSession);
    }

    private static async Task<Result<DetailedItemDto>> GetDetails(
        [FromRoute] Guid itemId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var response = await bus.InvokeAsync<Result<DetailedItemDto>>(new GetItemDetailsQuery(itemId), ct);
        return response;
    }
    private static async Task<Result<CurrentSessionOverviewDto>> GetCurrentSessionDetails(
        [FromRoute] Guid itemId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var res = await bus.InvokeAsync<Result<CurrentSessionOverviewDto>>(
            new GetCurrentSessionDetailsQuery(itemId), ct);

        return res;
    }

    private static async Task<Result> StartSession(
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

        var res = await bus.InvokeAsync<Result>(cmd, ct);
        return res;
    }

    private static async Task<Result> PauseSession(
        [FromRoute] Guid itemId,
        [FromBody] PauseSessionDto dto,
        IMessageBus bus,
        CancellationToken ct)
    {
        var cmd = new PauseSessionCommand(
            dto.ItemId,
            dto.PauseReason
        );

        var res = await bus.InvokeAsync<Result>(cmd, ct);
        return res;
    }
    private static async Task<Result> ContinueSession(
        [FromRoute] Guid itemId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var cmd = new ContinueSessionCommand(itemId);

        var res = await bus.InvokeAsync<Result>(cmd, ct);
        return res;
    }
    private static async Task<Result> StopSession(
        [FromRoute] Guid itemId,
        [FromBody] StopSessionDto dto,
        IMessageBus bus,
        CancellationToken ct)
    {
        var cmd = new StopSessionCommand(
            dto.ItemId,
            dto.Efficiency,
            dto.Concentration,
            dto.Summary
        );

        var res = await bus.InvokeAsync<Result>(cmd, ct);
        return res;
    }
}
