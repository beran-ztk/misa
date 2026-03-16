using Misa.Application.Features.Dev;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Dev;

public static class DevEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost(DevRoutes.SeedData,       Seed);
        api.MapPost(DevRoutes.SeedStressData, SeedStress);
        api.MapDelete(DevRoutes.DeleteData,   DeleteData);
    }

    private static async Task<IResult> Seed(IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new SeedDevDataCommand(), ct);
        return Results.Ok();
    }

    private static async Task<IResult> SeedStress(IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new SeedStressDataCommand(), ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteData(IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new DeleteUserDataCommand(), ct);
        return Results.NoContent();
    }
}
