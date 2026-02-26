using Misa.Contract.Items.Components.Tasks;

namespace Misa.Application.Features.Items.Tasks;

public sealed record UpdateTaskCategoryCommand(Guid ItemId, TaskCategoryDto Category);

public sealed class UpdateTaskCategoryHandler
{
    
}