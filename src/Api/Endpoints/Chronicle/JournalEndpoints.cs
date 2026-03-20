using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Chronicle;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Chronicle;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Chronicle;

public static class ChronicleEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost(ChronicleRoutes.CreateJournal, Create);
        api.MapPut(ChronicleRoutes.UpdateJournal,  Update);
        api.MapGet(ChronicleRoutes.GetChronicle,   GetAll);
    }

    private static async Task<IResult> Create(
        [FromBody] CreateJournalRequest request,
        IMessageBus bus)
    {
        var command = new CreateJournalCommand(
            request.Title,
            request.Description,
            request.OccurredAtUtc,
            request.UntilAtUtc
        );

        await bus.InvokeAsync(command);

        return Results.Ok();
    }

    private static async Task<IResult> Update(
        [FromRoute] Guid itemId,
        [FromBody]  UpdateJournalRequest request,
        IMessageBus bus)
    {
        await bus.InvokeAsync(new UpdateJournalCommand(itemId, request.Description, request.OccurredAtUtc));
        return Results.Ok();
    }

    private static async Task<IResult> GetAll(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<List<ChronicleEntryDto>>(new GetChronicleQuery(from, to));
        return Results.Ok(dto);
    }
}
