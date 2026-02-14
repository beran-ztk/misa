using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Queries;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Tasks;

public static class GetTasksEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("tasks", GetTasks);
    }
    private static async Task<Result<List<TaskDto>>> GetTasks(IMessageBus bus, CancellationToken ct)
    {
        try
        {
            var result = await bus.InvokeAsync<Result<List<TaskDto>>>(new GetTasksQuery(), ct);

            return result;
        }
        catch (Exception ex)
        {
            return Result<List<TaskDto>>.Invalid("", ex.ToString());
        }
    }
}