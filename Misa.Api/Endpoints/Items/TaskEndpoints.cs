using Misa.Application.Items.Queries;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Common;
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