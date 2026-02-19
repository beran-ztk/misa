using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Misa.Api.Services.Auth;
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
        var command = new CreateTaskCommand(
            dto.Title,
            dto.CategoryDto,
            dto.PriorityDto);

        var result = await bus.InvokeAsync<Result<TaskDto>>(command, ct);
        return result;
    }
}
