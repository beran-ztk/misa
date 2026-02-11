using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;

namespace Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;

public record AddTaskDto(string Title, TaskCategoryContract CategoryContract, PriorityDto PriorityDto, DeadlineInputDto? Deadline);