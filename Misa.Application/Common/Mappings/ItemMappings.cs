using Misa.Contract.Features.Entities.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;

namespace Misa.Application.Common.Mappings;
public static class ItemMappings
{
    public static ItemDto ToDto(this Item item)
    {
        return new ItemDto
        {
            Id = item.Id,
            StateId = item.StateId,
            Priority = item.Priority.MapToDto(),
            Title = item.Title,
            Entity = item.Entity.ToDto()
        };
    }
    public static Priority MapToDomain(this PriorityContract priorityContract) =>
        priorityContract switch
        {
            PriorityContract.None => Priority.None,
            PriorityContract.Low => Priority.Low,
            PriorityContract.Medium => Priority.Medium,
            PriorityContract.High => Priority.High,
            PriorityContract.Urgent => Priority.Urgent,
            PriorityContract.Critical => Priority.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(priorityContract), priorityContract, null)
        };
    public static PriorityContract MapToDto(this Priority priority) =>
        priority switch
        {
            Priority.None     => PriorityContract.None,
            Priority.Low      => PriorityContract.Low,
            Priority.Medium   => PriorityContract.Medium,
            Priority.High     => PriorityContract.High,
            Priority.Urgent   => PriorityContract.Urgent,
            Priority.Critical => PriorityContract.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
        };
    public static WorkflowContract MapToDto(this Workflow workflow) =>
        workflow switch
        {
            Workflow.Task => WorkflowContract.Task,
            Workflow.Deadline => WorkflowContract.Deadline,
            _ => throw new ArgumentOutOfRangeException(nameof(workflow), workflow, null)
        };
}