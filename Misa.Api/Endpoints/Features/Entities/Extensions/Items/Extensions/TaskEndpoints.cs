using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Commands;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Wolverine;

namespace Misa.Api.Endpoints.Features.Entities.Extensions.Items.Extensions;

public static class TaskEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("tasks", AddTask);
    }
    private static async Task<Result<TaskDto>> AddTask([FromBody] AddTaskDto dto, IMessageBus bus, CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            var command = new AddTaskCommand(dto.Title, dto.CategoryContract, dto.PriorityContract);
            var result = await bus.InvokeAsync<Result<TaskDto>>(command, linkedCts.Token);

            return result;
        }
        catch (Exception ex)
        {
            return Result<TaskDto>.Invalid("", "");
        }
    }
}