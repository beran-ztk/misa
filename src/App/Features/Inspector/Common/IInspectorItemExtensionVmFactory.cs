using System.Text.Json;
using Misa.Contract.Features.Entities.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Base;

namespace Misa.Ui.Avalonia.Features.Inspector.Common;

public interface IInspectorItemExtensionVmFactory
{
    IItemExtensionVm? Create(DetailedItemDto dto);
}

public sealed class InspectorItemExtensionVmFactory : IInspectorItemExtensionVmFactory
{
    public IItemExtensionVm? Create(DetailedItemDto dto)
    {
        if (dto.Extension is null)
            return null;

        return dto.Kind switch
        {
            WorkflowDto.Task => new TaskExtensionVm(dto.Extension.Value.Deserialize<TaskDetailsDto>()!),
            _ => null
        };
    }
}