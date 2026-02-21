using Misa.Application.Features.Items.Tasks.Queries;
using Misa.Contract.Items.Components.Tasks;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Tasks;

public static class GetTasksEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapGet("tasks", GetTasks);
    }
    private static async Task<Result<IReadOnlyCollection<TaskExtensionDto>>> GetTasks(
        IMessageBus bus, 
        CancellationToken ct)
    {
        return await bus.InvokeAsync<Result<IReadOnlyCollection<TaskExtensionDto>>>(new GetTasksQuery(), ct);
    }
}