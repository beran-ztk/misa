using Misa.Contract.Features.Entities.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Domain.Features.Entities.Base;
using DomainCategory = Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks.TaskCategory;
using DomainPriority = Misa.Domain.Features.Entities.Extensions.Items.Base.Priority;

namespace Misa.Application.Features.Entities.Extensions.Items.Mappings;

public static class ItemMappings
{
    public static DomainCategory MapToDomain(this TaskCategoryContract categoryContract) =>
        categoryContract switch
        {
            TaskCategoryContract.Personal => DomainCategory.Personal,
            TaskCategoryContract.School => DomainCategory.School,
            TaskCategoryContract.Work => DomainCategory.Work,
            TaskCategoryContract.Other => DomainCategory.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(categoryContract), categoryContract, null)
        };
    public static DomainPriority MapToDomain(this PriorityContract priorityContract) =>
        priorityContract switch
        {
            PriorityContract.None => DomainPriority.None,
            PriorityContract.Low => DomainPriority.Low,
            PriorityContract.Medium => DomainPriority.Medium,
            PriorityContract.High => DomainPriority.High,
            PriorityContract.Urgent => DomainPriority.Urgent,
            PriorityContract.Critical => DomainPriority.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(priorityContract), priorityContract, null)
        };
    public static TaskCategoryContract MapToDto(this DomainCategory domainCategory) =>
        domainCategory switch
        {
            DomainCategory.Personal => TaskCategoryContract.Personal,
            DomainCategory.School   => TaskCategoryContract.School,
            DomainCategory.Work     => TaskCategoryContract.Work,
            DomainCategory.Other    => TaskCategoryContract.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(domainCategory), domainCategory, null)
        };

    public static PriorityContract MapToDto(this DomainPriority domainPriority) =>
        domainPriority switch
        {
            DomainPriority.None     => PriorityContract.None,
            DomainPriority.Low      => PriorityContract.Low,
            DomainPriority.Medium   => PriorityContract.Medium,
            DomainPriority.High     => PriorityContract.High,
            DomainPriority.Urgent   => PriorityContract.Urgent,
            DomainPriority.Critical => PriorityContract.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(domainPriority), domainPriority, null)
        };
    public static WorkflowContract MapToDto(this Workflow workflow) =>
        workflow switch
        {
            Workflow.Task => WorkflowContract.Task,
            Workflow.Deadline => WorkflowContract.Deadline,
            _ => throw new ArgumentOutOfRangeException(nameof(workflow), workflow, null)
        };
}