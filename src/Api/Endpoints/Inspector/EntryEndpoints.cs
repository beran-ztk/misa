using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Inspector;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Inspector;

public static class EntryEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch(ItemRoutes.ArchiveItem,    Archive);
        endpoints.MapPatch(ItemRoutes.RestoreItem,    Restore);
        endpoints.MapDelete(ItemRoutes.DeleteItem,    Delete);
        endpoints.MapDelete(ItemRoutes.HardDeleteItem, HardDelete);
    }

    private static async Task<IResult> Archive(
        [FromRoute] Guid itemId,
        IMessageBus bus)
    {
        await bus.InvokeAsync(new ArchiveItemCommand(itemId));
        return Results.Ok();
    }

    private static async Task<IResult> Restore(
        [FromRoute] Guid itemId,
        IMessageBus bus)
    {
        await bus.InvokeAsync(new RestoreItemCommand(itemId));
        return Results.Ok();
    }

    private static async Task<IResult> Delete(
        [FromRoute] Guid itemId,
        IMessageBus bus)
    {
        await bus.InvokeAsync(new DeleteItemCommand(itemId));
        return Results.Ok();
    }

    private static async Task<IResult> HardDelete(
        [FromRoute] Guid itemId,
        IMessageBus bus)
    {
        await bus.InvokeAsync(new HardDeleteItemCommand(itemId));
        return Results.Ok();
    }
}