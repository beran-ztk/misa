using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Commands;
using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Queries;
using Misa.Application.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Wolverine;

namespace Misa.Api.Endpoints.Features.Entities.Extensions.Items.Extensions;

public static class TaskEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("tasks", GetTasks);
        app.MapPost("tasks", AddTask);
    }
    private static async Task<Result<TaskDto>> AddTask([FromBody] AddTaskDto dto, IMessageBus bus, CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            var command = dto.ToCommand();
            var result = await bus.InvokeAsync<Result<TaskDto>>(command, linkedCts.Token);

            return result;
        }
        catch (Exception ex)
        {
            return Result<TaskDto>.Invalid("", ex.ToString());
        }
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