using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Queries;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Wolverine;

namespace Misa.Api.Endpoints.Items;

public static class TaskEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/items/tasks", GetTasks);
    }

    private static async Task<Result<List<ListTaskDto>>> GetTasks(IMessageBus bus, CancellationToken ct)
    {
        var res = await bus.InvokeAsync<Result<List<ListTaskDto>>>(new GetTasksQuery(), ct);
        return res;
    }
}