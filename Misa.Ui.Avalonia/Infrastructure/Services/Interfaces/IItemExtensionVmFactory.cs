using System.Text.Json;
using Misa.Contract.Features.Entities.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Ui.Avalonia.Features.Details.Common;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;

public interface IItemExtensionVmFactory
{
    IItemExtensionVm? Create(DetailedItemDto dto);
}

public sealed class ItemExtensionVmFactory : IItemExtensionVmFactory
{
    public IItemExtensionVm? Create(DetailedItemDto dto)
    {
        if (dto.Extension is null)
            return null;

        return dto.Kind switch
        {
            WorkflowContract.Task => new TaskExtensionVm(dto.Extension.Value.Deserialize<TaskDetailsDto>()!),
            _ => null
        };
    }
}