using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Commands;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Tasks;

public static class CreateTaskEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost("tasks", AddTask);
    }

    private static async Task<Result<TaskDto>> AddTask(
        [FromBody] CreateTaskDto dto,
        IMessageBus bus,
        CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        var command = new CreateTaskCommand(
            dto.Title,
            dto.CategoryDto,
            dto.PriorityDto);

        return await bus.InvokeAsync<Result<TaskDto>>(command, linkedCts.Token);
    }
}
