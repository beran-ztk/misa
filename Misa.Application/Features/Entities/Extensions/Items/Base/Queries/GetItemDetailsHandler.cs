using System.Text.Json;
using System.Text.Json.Serialization;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Common.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Base;

namespace Misa.Application.Features.Entities.Extensions.Items.Base.Queries;
public record GetItemDetailsQuery(Guid Id);
public sealed class GetItemDetailsHandler(IItemRepository repository)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    public async Task<Result<DetailedItemDto>> HandleAsync(GetItemDetailsQuery cmd, CancellationToken ct)
    {
        var item = await repository.TryGetItemDetailsAsync(cmd.Id, ct);
        if (item == null)
            return Result<DetailedItemDto>.NotFound("Item-Details", "Details not found.");
        
        var itemDto = item.ToDto();
        var kind = item.Entity.Workflow;

        var extension = kind switch
        {
            Workflow.Task => await BuildTaskExtensionAsync(item.Id, ct),
            _ => null
        };
        
        var detailedItemDto = new DetailedItemDto
        {
            Kind = itemDto.Entity.Workflow,
            Item = itemDto,
            Extension = extension
        };
        
        return Result<DetailedItemDto>.Ok(detailedItemDto);
    }

    private async Task<JsonElement?> BuildTaskExtensionAsync(Guid itemId, CancellationToken ct)
    {
        var task = await repository.TryGetTaskAsync(itemId, ct);
        if (task == null)
            return null;

        var dto = new TaskDetailsDto
        {
            Category = task.Category.MapToDto()
        };
        
        return JsonSerializer.SerializeToElement(dto, JsonOptions);
    }
}