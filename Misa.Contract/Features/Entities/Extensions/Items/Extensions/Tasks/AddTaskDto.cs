using Misa.Contract.Features.Entities.Extensions.Items.Base;

namespace Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;

public record AddTaskDto(string Title, TaskCategoryContract CategoryContract, PriorityContract PriorityContract);