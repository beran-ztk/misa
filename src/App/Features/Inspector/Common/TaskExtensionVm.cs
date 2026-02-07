using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;

namespace Misa.Ui.Avalonia.Features.Inspector.Common;

public sealed class TaskExtensionVm(TaskDetailsDto dto) : IItemExtensionVm
{
    public TaskCategoryContract Category { get; } = dto.Category;
}