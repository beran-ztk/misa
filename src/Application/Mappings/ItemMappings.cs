using Misa.Contract.Features.Entities.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities;

namespace Misa.Application.Mappings;
public static class ItemMappings
{
    public static ActivityPriority MapToDomain(this PriorityDto priorityDto) =>
        priorityDto switch
        {
            PriorityDto.None => ActivityPriority.None,
            PriorityDto.Low => ActivityPriority.Low,
            PriorityDto.Medium => ActivityPriority.Medium,
            PriorityDto.High => ActivityPriority.High,
            PriorityDto.Urgent => ActivityPriority.Urgent,
            PriorityDto.Critical => ActivityPriority.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(priorityDto), priorityDto, null)
        };
    public static PriorityDto MapToDto(this ActivityPriority activityPriority) =>
        activityPriority switch
        {
            ActivityPriority.None     => PriorityDto.None,
            ActivityPriority.Low      => PriorityDto.Low,
            ActivityPriority.Medium   => PriorityDto.Medium,
            ActivityPriority.High     => PriorityDto.High,
            ActivityPriority.Urgent   => PriorityDto.Urgent,
            ActivityPriority.Critical => PriorityDto.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(activityPriority), activityPriority, null)
        };
    public static WorkflowDto MapToDto(this Workflow workflow) =>
        workflow switch
        {
            Workflow.Task => WorkflowDto.Task,
            Workflow.Schedule => WorkflowDto.Scheduling,
            _ => throw new ArgumentOutOfRangeException(nameof(workflow), workflow, null)
        };
}