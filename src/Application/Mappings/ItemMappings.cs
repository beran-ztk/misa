using Misa.Contract.Items;
using Misa.Domain.Items;

namespace Misa.Application.Mappings;
public static class ItemMappings
{
    public static ItemDto ToDto(this Item item)
    {
        return new ItemDto
        {
            Id = item.Id.Value,
            OwnerId = item.OwnerId,
            Workflow = item.Workflow.ToDto(),

            Title = item.Title,
            Description = item.Description,

            IsDeleted = item.IsDeleted,
            IsArchived = item.IsArchived,

            CreatedAt = item.CreatedAt,
            ModifiedAt = item.ModifiedAt
        };
    }
    public static WorkflowDto ToDto(this Workflow workflow) =>
        workflow switch
        {
            Workflow.Task => WorkflowDto.Task,
            Workflow.Schedule => WorkflowDto.Scheduling,
            _ => throw new ArgumentOutOfRangeException(nameof(workflow), workflow, null)
        };
}