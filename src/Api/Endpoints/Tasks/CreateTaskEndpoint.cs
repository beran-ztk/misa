using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Tasks.Commands;
using Misa.Contract.Items.Components.Tasks;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Tasks;

public static class CreateTaskEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost("tasks", AddTask);
    }

    private static async Task<Result<TaskExtensionDto>> AddTask(
        [FromBody] CreateTaskDto dto,
        IMessageBus bus, 
        CancellationToken ct)
    {
        var command = new CreateTaskCommand(
            dto.Title,
            dto.Description,
            dto.CategoryDto,
            dto.ActivityPriorityDto,
            dto.DueDate
        );

        var result = await bus.InvokeAsync<Result<TaskExtensionDto>>(command, ct);
        return result;
    }
}
