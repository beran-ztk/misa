using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Inspector;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Inspector;

public static class EntryEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch(ItemRoutes.ArchiveItem, Archive);
        endpoints.MapDelete(ItemRoutes.DeleteItem, Delete);
    }
    private static async Task<IResult> Archive(
        [FromRoute] Guid itemId,
        IMessageBus bus)
    {
        await bus.InvokeAsync(new ArchiveItemCommand(itemId));

        return Results.Ok();
    }    
    
    private static async Task<IResult> Delete(
        [FromRoute] Guid itemId,
        IMessageBus bus)
    {
        await bus.InvokeAsync(new DeleteItemCommand(itemId));

        return Results.Ok();
    }    
}