using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;

namespace Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;

public record CreateTaskDto(string Title, TaskCategoryDto CategoryDto, PriorityDto PriorityDto, DeadlineInputDto? Deadline);