using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Tasks;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Tasks;

public static class TaskEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost(TaskRoutes.CreateTask, Create);
        api.MapGet(TaskRoutes.GetTask, GetSingle);
        api.MapGet(TaskRoutes.GetTasks, GetAll);
        api.MapPatch(TaskRoutes.UpdateTaskCategory, UpdateCategory);
        api.MapDelete(TaskRoutes.DeleteTask, Delete);
    }
    
    // Create a task
    private static async Task<IResult> Create(
        [FromBody] CreateTaskRequest request, 
        IMessageBus bus, 
        CancellationToken ct)
    {
        var command = new CreateTaskCommand(
            request.Title,
            request.Description,
            request.CategoryDto,
            request.ActivityPriorityDto,
            request.DueDate
        );

        var dto = await bus.InvokeAsync<TaskDto>(command, ct);
        
        return Results.Created(TaskRoutes.GetTaskRequest(dto.Item.Id), dto);
    }
    
    // Get task
    private static async Task<IResult> GetSingle(
        [FromRoute] Guid itemId, 
        IMessageBus bus, 
        CancellationToken ct)
    {
        var dto = await bus.InvokeAsync<TaskDto?>(new GetTaskQuery(itemId), ct);
        return Results.Ok(dto);
    }   
    
    // Get tasks
    private static async Task<IResult> GetAll(IMessageBus bus, CancellationToken ct)
    {
        var dto = await bus.InvokeAsync<List<TaskDto>>(new GetTasksQuery(), ct);
        
        return Results.Ok(dto);
    }   
    
    // Update a task
    private static async Task<Result> UpdateCategory(
        [FromRoute] Guid itemId, 
        [FromBody] UpdateTaskCategoryRequest request,
        IMessageBus bus, 
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<Result>(new UpdateTaskCategoryCommand(itemId, request.CategoryDto) , ct);
        return result;
    }    
    
    // Delete a task
    private static async Task<Result> Delete(
        [FromRoute] Guid itemId,
        IMessageBus bus, 
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<Result>(new DeleteTaskCommand(itemId), ct);
        return result;
    }
}