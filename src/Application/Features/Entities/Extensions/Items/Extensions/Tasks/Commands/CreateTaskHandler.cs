using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Shared.Results;
using Misa.Domain.Items.Components.Tasks;

namespace Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Commands;
public sealed record CreateTaskCommand(
    string Title,
    TaskCategoryDto CategoryDto,
    PriorityDto PriorityDto
);
public class CreateTaskHandler(
    ITaskRepository repository,
    ITimeProvider timeProvider, 
    IIdGenerator idGenerator,
    ICurrentUser currentUser)
{
    public async Task<Result<TaskDto>> HandleAsync(CreateTaskCommand command, CancellationToken ct)
    {
        var task = TaskExtension.Create(
            idGenerator.New(), 
            ownerId: currentUser.UserId,
            command.Title, 
            command.CategoryDto.MapToDomain(), 
            command.PriorityDto.MapToDomain(),
            timeProvider.UtcNow
        );
        
        await repository.AddAsync(task, ct);
        await repository.SaveChangesAsync(ct);

        var formattedTask = task.ToDto();
        
        return Result<TaskDto>
            .Ok(formattedTask);
    }
}