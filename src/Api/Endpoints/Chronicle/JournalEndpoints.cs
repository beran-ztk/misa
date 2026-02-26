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
        api.MapGet(ChronicleRoutes.GetJournals, GetAll);
    }

    // Create a task
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

    // Get tasks
    private static async Task<IResult> GetAll(IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<List<ItemDto>>(new GetJournalsCommand());

        return Results.Ok(dto);
    }
}